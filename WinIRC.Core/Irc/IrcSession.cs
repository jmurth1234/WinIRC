using System.Collections.Specialized;
using IrcClientCore;
using IrcClientCore.Handlers.BuiltIn;
using WinIRC.Core.Configuration;
using WinIRC.Core.Dispatching;
using WinIRC.Core.Events;
using WinIRC.Core.Logging;
using WinIRC.Core.Models;
using WinIRC.Core.Networking;

namespace WinIRC.Core.Irc;

public sealed class IrcSession : IDisposable
{
    public const string ServerBufferName = "Server";

    private readonly ISettingsStore settings;
    private readonly HashSet<string> knownChannels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, INotifyCollectionChanged> watchedMessageCollections = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, INotifyCollectionChanged> watchedUserCollections = new(StringComparer.OrdinalIgnoreCase);
    private MessageLogWriter? logWriter;
    private int refreshScheduled;
    private bool initialised;

    public IrcSession(WinIrcServer server, IIrcConnectionFactory connectionFactory, ISettingsStore settings, IAppDispatcher? dispatcher = null)
    {
        this.settings = settings;
        server.IgnoreCertErrors = settings.GetBoolean(ConfigKeys.IgnoreSsl);
        server.ShouldReconnect = settings.GetBoolean(ConfigKeys.AutoReconnect, true);
        Client = new CoreIrcClient(server, connectionFactory, dispatcher ?? new ImmediateDispatcher());
        Client.HandleDisconnect = _ => Raise(new IrcSessionEventArgs(IrcSessionEventKind.Disconnected));
        Client.HandleDisplayChannelList = HandleChannelList;
        Client.StateChanged += (_, _) => ScheduleRefreshFromClient();
    }

    public event EventHandler<IrcSessionEventArgs>? SessionEvent;

    public CoreIrcClient Client { get; }
    public WinIrcServer Server => Client.Server;
    public bool IsConnected => Client.IsConnected;
    public bool IsConnecting => Client.IsConnecting;
    public string CurrentChannel { get; private set; } = ServerBufferName;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitialisedAsync(cancellationToken);

        Server.IgnoreCertErrors = settings.GetBoolean(ConfigKeys.IgnoreSsl);
        Server.ShouldReconnect = settings.GetBoolean(ConfigKeys.AutoReconnect, true);
        Raise(new IrcSessionEventArgs(IrcSessionEventKind.StatusChanged, text: "Connecting"));
        Client.Connect();
    }

    public Task DisconnectAsync(string message = "Powered by WinIRC", CancellationToken cancellationToken = default)
    {
        Client.DisconnectAsync(message, attemptReconnect: false);
        Raise(new IrcSessionEventArgs(IrcSessionEventKind.Disconnected));
        return Task.CompletedTask;
    }

    public Task SendAsync(string? channel, string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Task.CompletedTask;
        }

        var target = string.IsNullOrWhiteSpace(channel) || channel == ServerBufferName
            ? CurrentChannel
            : channel;

        if (target == ServerBufferName && !input.StartsWith('/'))
        {
            AddClientMessage(ServerBufferName, "Join a channel or use a command before sending a normal message.");
            return Task.CompletedTask;
        }

        Client.CommandManager.HandleCommand(target == ServerBufferName ? "" : target, input);
        Client.NotifyStateChanged();
        return Task.CompletedTask;
    }

    public Task RequestChannelListAsync(CancellationToken cancellationToken = default)
    {
        Client.CommandManager.HandleCommand("", "/list");
        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetChannelNames()
    {
        RefreshFromClient();
        return knownChannels.OrderBy(channel => channel == ServerBufferName ? "" : channel, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task EnsureInitialisedAsync(CancellationToken cancellationToken = default)
    {
        if (initialised)
        {
            return;
        }

        Client.Initialise();
        initialised = true;

        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (Client.ChannelList?.ServerLog?.Buffers != null)
            {
                RefreshFromClient();
                return;
            }

            await Task.Delay(10, cancellationToken);
        }

        RefreshFromClient();
    }

    public IReadOnlyList<Message> GetMessages(string channel)
    {
        var buffer = GetBuffer(channel);
        return buffer == null ? Array.Empty<Message>() : buffer.Collection.ToList();
    }

    public IReadOnlyList<string> GetUsers(string channel)
    {
        if (channel == ServerBufferName || !Client.ChannelList.Contains(channel))
        {
            return Array.Empty<string>();
        }

        return Client.GetRawUsers(channel).OrderBy(user => user, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public string GetTopic(string channel)
    {
        return channel == ServerBufferName ? "Server log" : Client.GetChannelTopic(channel);
    }

    public IReadOnlyList<string> GetTabCompletions(string channel, string message, int caretPosition)
    {
        var context = GetCurrentWordContext(message, caretPosition);
        var word = context.Word;
        if (string.IsNullOrWhiteSpace(word))
        {
            return Array.Empty<string>();
        }

        if (message.StartsWith("/", StringComparison.Ordinal))
        {
            if (context.WordIndex == 0)
            {
                return Client.CommandManager.CommandList
                    .Select(command => command.StartsWith("/", StringComparison.Ordinal) ? command : "/" + command)
                    .Where(command => command.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(command => command, StringComparer.OrdinalIgnoreCase)
                    .Take(20)
                    .ToList();
            }

            var command = message.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            var commandCompletions = Client.CommandManager
                .GetCompletions(channel == ServerBufferName ? "" : channel, command, word)
                .Where(completion => !string.IsNullOrWhiteSpace(completion))
                .Take(20)
                .ToList();
            if (commandCompletions.Count > 0)
            {
                return commandCompletions;
            }
        }

        if (channel == ServerBufferName)
        {
            return Array.Empty<string>();
        }

        return GetUsers(channel)
            .Where(user => user.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToList();
    }

    public void SelectChannel(string channel)
    {
        if (Client.ChannelList == null)
        {
            return;
        }

        foreach (Channel existing in Client.ChannelList)
        {
            existing.CurrentlyViewing = string.Equals(
                existing.ServerLog ? ServerBufferName : existing.Name,
                channel,
                StringComparison.OrdinalIgnoreCase);

            if (existing.CurrentlyViewing)
            {
                existing.HasUnread = false;
                existing.HasMentions = false;
                existing.UnreadCount = 0;
                CurrentChannel = existing.ServerLog ? ServerBufferName : existing.Name;
            }
        }
    }

    public int GetUnreadCount(string channel)
    {
        var state = GetChannel(channel);
        return state?.UnreadCount ?? 0;
    }

    public bool HasMentions(string channel)
    {
        var state = GetChannel(channel);
        return state?.HasMentions ?? false;
    }

    public void Dispose()
    {
        foreach (var collection in watchedMessageCollections.Values)
        {
            collection.CollectionChanged -= MessageCollection_CollectionChanged;
        }

        foreach (var collection in watchedUserCollections.Values)
        {
            collection.CollectionChanged -= UserCollection_CollectionChanged;
        }

        (Client.Connection as IDisposable)?.Dispose();
        logWriter?.Dispose();
    }

    private void RefreshFromClient()
    {
        var channelNames = new List<string> { ServerBufferName };

        if (Client.ChannelList != null)
        {
            if (Client.ChannelList.ServerLog != null)
            {
                WatchChannel(ServerBufferName, Client.ChannelList.ServerLog);
            }

            foreach (Channel channel in Client.ChannelList)
            {
                channelNames.Add(channel.ServerLog ? ServerBufferName : channel.Name);
                WatchChannel(channel.ServerLog ? ServerBufferName : channel.Name, channel);
            }
        }

        foreach (var channel in channelNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (knownChannels.Add(channel))
            {
                Raise(new IrcSessionEventArgs(IrcSessionEventKind.ChannelAdded, channel));
            }
        }

        foreach (var removed in knownChannels.Except(channelNames, StringComparer.OrdinalIgnoreCase).ToList())
        {
            knownChannels.Remove(removed);
            Raise(new IrcSessionEventArgs(IrcSessionEventKind.ChannelRemoved, removed));
        }
    }

    private void ScheduleRefreshFromClient()
    {
        if (Interlocked.Exchange(ref refreshScheduled, 1) == 1)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(75);
            await Client.Dispatcher.InvokeAsync(() =>
            {
                Interlocked.Exchange(ref refreshScheduled, 0);
                RefreshFromClient();
                Raise(new IrcSessionEventArgs(IrcSessionEventKind.StatusChanged, text: Client.IsConnected ? "Connected" : "Ready"));
            });
        });
    }

    private void WatchChannel(string channelName, Channel channel)
    {
        if (channel.Buffers is IrcClientCore.Buffer buffer && !watchedMessageCollections.ContainsKey(channelName))
        {
            watchedMessageCollections[channelName] = buffer.Collection;
            buffer.Collection.CollectionChanged += MessageCollection_CollectionChanged;
        }

        if (channel.Store?.RawUsers != null && !watchedUserCollections.ContainsKey(channelName))
        {
            watchedUserCollections[channelName] = channel.Store.RawUsers;
            channel.Store.RawUsers.CollectionChanged += UserCollection_CollectionChanged;
        }
    }

    private void MessageCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null)
        {
            return;
        }

        var channel = watchedMessageCollections.FirstOrDefault(item => ReferenceEquals(item.Value, sender)).Key;
        if (string.IsNullOrWhiteSpace(channel))
        {
            channel = ServerBufferName;
        }

        foreach (Message message in e.NewItems)
        {
            LogMessage(channel, message);
            Raise(new IrcSessionEventArgs(IrcSessionEventKind.MessageAdded, channel, message));
        }
    }

    private void UserCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var channel = watchedUserCollections.FirstOrDefault(item => ReferenceEquals(item.Value, sender)).Key;
        if (!string.IsNullOrWhiteSpace(channel))
        {
            Raise(new IrcSessionEventArgs(IrcSessionEventKind.UsersUpdated, channel, users: GetUsers(channel)));
        }
    }

    private void HandleChannelList(List<ChannelListItem> items)
    {
        var entries = items
            .Select(item => new ChannelListEntry(item.Channel, item.Topic, item.Users))
            .OrderByDescending(item => item.Users)
            .ThenBy(item => item.Channel, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Raise(new IrcSessionEventArgs(IrcSessionEventKind.ChannelListReceived, channelList: entries));
    }

    private IrcClientCore.Buffer? GetBuffer(string channel)
    {
        RefreshFromClient();

        if (channel == ServerBufferName)
        {
            return Client.ChannelList?.ServerLog?.Buffers as IrcClientCore.Buffer;
        }

        return Client.ChannelList != null && Client.ChannelList.Contains(channel)
            ? Client.ChannelList[channel].Buffers as IrcClientCore.Buffer
            : null;
    }

    private Channel? GetChannel(string channel)
    {
        if (Client.ChannelList == null)
        {
            return null;
        }

        if (channel == ServerBufferName)
        {
            return Client.ChannelList.ServerLog;
        }

        return Client.ChannelList.Contains(channel) ? Client.ChannelList[channel] : null;
    }

    private void AddClientMessage(string channel, string text)
    {
        Client.AddClientMessage(channel, text);
    }

    private void LogMessage(string channel, Message message)
    {
        if (!settings.GetBoolean(ConfigKeys.EnableLogs))
        {
            return;
        }

        logWriter ??= new MessageLogWriter(Server.Name, settings.GetString(ConfigKeys.LogsFolder));
        logWriter.Add(channel, message);
    }

    private void Raise(IrcSessionEventArgs args)
    {
        SessionEvent?.Invoke(this, args);
    }

    private static (string Word, int WordIndex) GetCurrentWordContext(string message, int caretPosition)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return ("", -1);
        }

        caretPosition = Math.Clamp(caretPosition, 0, message.Length);
        var start = caretPosition;
        while (start > 0 && !char.IsWhiteSpace(message[start - 1]))
        {
            start--;
        }

        var index = message[..start]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Length;

        return (message[start..caretPosition], index);
    }
}
