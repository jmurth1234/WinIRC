using Avalonia;
using Avalonia.Collections;
using Avalonia.Styling;
using Avalonia.Threading;
using IrcClientCore;
using System.Text;
using WinIRC.Avalonia.Services;
using WinIRC.Core.Configuration;
using WinIRC.Core.Events;
using WinIRC.Core.Irc;
using WinIRC.Core.Models;
using WinIRC.Core.Networking;
using WinIRC.Core.Persistence;

namespace WinIRC.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private static readonly HttpClient UploadClient = new();

    private readonly IrcConnectionFactoryHolder connectionFactoryHolder = new();
    private readonly List<IrcSession> sessions = new();
    private readonly List<PendingMessage> pendingCurrentMessages = new();
    private readonly HashSet<IrcSession> pendingSidebarRefreshes = new();
    private readonly List<string> inputHistory = new();
    private readonly List<ChannelListEntry> channelListEntries = new();
    private SidebarItemViewModel? selectedItem;
    private IrcSession? channelListSession;
    private bool refreshingCurrentBuffer;
    private int messageFlushScheduled;
    private int sidebarRefreshScheduled;
    private int inputHistoryIndex = -1;
    private int tabCompletionOriginalStart;
    private int tabCompletionOriginalEnd;
    private int tabCompletionLastCaret = -1;
    private int tabCompletionIndex = -1;
    private string currentTitle = "WinIRC";
    private string currentTopic = "Connect to a server to start chatting.";
    private string statusText = "Ready";
    private string inputText = "";
    private string channelListSearch = "";
    private string channelListStatus = "Request a channel list from the current server.";
    private string pendingHistoryInput = "";
    private string tabCompletionOriginalText = "";
    private string tabCompletionLastText = "";
    private IReadOnlyList<string> tabCompletionOptions = Array.Empty<string>();
    private bool showUsers;
    private bool showTopic;
    private bool ignoreJoinLeave;
    private bool darkTheme;
    private bool modernChat;
    private bool reducedPadding;
    private int chatFontSize;
    private string chatFontFamily;

    public MainWindowViewModel(JsonSettingsStore settings, ServerStore serverStore)
    {
        Settings = settings;
        ServerStore = serverStore;

        if (!Settings.Contains(ConfigKeys.AutoReconnect))
        {
            Settings.SetBoolean(ConfigKeys.AutoReconnect, true);
        }

        showUsers = Settings.GetBoolean(ConfigKeys.ShowUsersPane, true);
        showTopic = Settings.GetBoolean(ConfigKeys.ShowTopicBar, true);
        ignoreJoinLeave = Settings.GetBoolean(ConfigKeys.IgnoreJoinLeave);
        darkTheme = Settings.GetBoolean(ConfigKeys.DarkTheme);
        modernChat = Settings.GetBoolean(ConfigKeys.ModernChat);
        reducedPadding = Settings.GetBoolean(ConfigKeys.ReducedPadding);
        chatFontSize = Settings.GetInt(ConfigKeys.FontSize, 14);
        chatFontFamily = Settings.GetString(ConfigKeys.FontFamily, "Inter");
        if (string.IsNullOrWhiteSpace(chatFontFamily))
        {
            chatFontFamily = "Inter";
        }

        ApplyTheme(darkTheme);
    }

    public JsonSettingsStore Settings { get; }
    public ServerStore ServerStore { get; }
    public AvaloniaList<SidebarItemViewModel> SidebarItems { get; } = new();
    public AvaloniaList<MessageViewModel> CurrentMessages { get; } = new();
    public AvaloniaList<string> CurrentUsers { get; } = new();
    public AvaloniaList<ChannelListEntryViewModel> ChannelListItems { get; } = new();

    public SidebarItemViewModel? SelectedItem
    {
        get => selectedItem;
        set
        {
            if (SetProperty(ref selectedItem, value))
            {
                RefreshCurrentBuffer();
            }
        }
    }

    public string CurrentTitle
    {
        get => currentTitle;
        private set => SetProperty(ref currentTitle, value);
    }

    public string CurrentTopic
    {
        get => currentTopic;
        private set => SetProperty(ref currentTopic, value);
    }

    public string StatusText
    {
        get => statusText;
        private set => SetProperty(ref statusText, value);
    }

    public string InputText
    {
        get => inputText;
        set => SetProperty(ref inputText, value);
    }

    public string ChannelListSearch
    {
        get => channelListSearch;
        set
        {
            if (SetProperty(ref channelListSearch, value))
            {
                RefreshChannelList();
            }
        }
    }

    public string ChannelListStatus
    {
        get => channelListStatus;
        private set => SetProperty(ref channelListStatus, value);
    }

    public bool AutoReconnect
    {
        get => Settings.GetBoolean(ConfigKeys.AutoReconnect, true);
        set
        {
            Settings.SetBoolean(ConfigKeys.AutoReconnect, value);
            OnPropertyChanged();
        }
    }

    public bool IgnoreSsl
    {
        get => Settings.GetBoolean(ConfigKeys.IgnoreSsl);
        set
        {
            Settings.SetBoolean(ConfigKeys.IgnoreSsl, value);
            OnPropertyChanged();
        }
    }

    public bool SwitchOnJoin
    {
        get => Settings.GetBoolean(ConfigKeys.SwitchOnJoin, true);
        set
        {
            Settings.SetBoolean(ConfigKeys.SwitchOnJoin, value);
            OnPropertyChanged();
        }
    }

    public int UserListClickIndex
    {
        get => Math.Clamp(Settings.GetInt(ConfigKeys.UserListClick, 0), 0, 2);
        set
        {
            Settings.SetInt(ConfigKeys.UserListClick, Math.Clamp(value, 0, 2));
            OnPropertyChanged();
        }
    }

    public bool EnableLogs
    {
        get => Settings.GetBoolean(ConfigKeys.EnableLogs);
        set
        {
            Settings.SetBoolean(ConfigKeys.EnableLogs, value);
            OnPropertyChanged();
        }
    }

    public string LogsFolderPath
    {
        get => Settings.GetString(
            ConfigKeys.LogsFolder,
            Path.Combine(AppDataPaths.GetAppDataDirectory(), "logs"));
        set
        {
            Settings.SetString(ConfigKeys.LogsFolder, value);
            OnPropertyChanged();
        }
    }

    public string DefaultUsername
    {
        get => Settings.GetString(ConfigKeys.DefaultUsername);
        set
        {
            Settings.SetString(ConfigKeys.DefaultUsername, value);
            OnPropertyChanged();
        }
    }

    public string UploadFileUrl
    {
        get => Settings.GetString(ConfigKeys.UploadFile, "https://0x0.st");
        set
        {
            Settings.SetString(ConfigKeys.UploadFile, value);
            OnPropertyChanged();
        }
    }

    public string PasteTextUrl
    {
        get => Settings.GetString(ConfigKeys.PasteText, UploadFileUrl);
        set
        {
            Settings.SetString(ConfigKeys.PasteText, value);
            OnPropertyChanged();
        }
    }

    public string PasteImageUrl
    {
        get => Settings.GetString(ConfigKeys.PasteImage, UploadFileUrl);
        set
        {
            Settings.SetString(ConfigKeys.PasteImage, value);
            OnPropertyChanged();
        }
    }

    public bool ShowUsers
    {
        get => showUsers;
        set
        {
            if (SetProperty(ref showUsers, value))
            {
                Settings.SetBoolean(ConfigKeys.ShowUsersPane, value);
            }
        }
    }

    public bool ShowTopic
    {
        get => showTopic;
        set
        {
            if (SetProperty(ref showTopic, value))
            {
                Settings.SetBoolean(ConfigKeys.ShowTopicBar, value);
            }
        }
    }

    public bool IgnoreJoinLeave
    {
        get => ignoreJoinLeave;
        set
        {
            if (SetProperty(ref ignoreJoinLeave, value))
            {
                Settings.SetBoolean(ConfigKeys.IgnoreJoinLeave, value);
                RefreshCurrentBuffer();
            }
        }
    }

    public bool DarkTheme
    {
        get => darkTheme;
        set
        {
            if (SetProperty(ref darkTheme, value))
            {
                Settings.SetBoolean(ConfigKeys.DarkTheme, value);
                ApplyTheme(value);
                RefreshCurrentBuffer();
            }
        }
    }

    public bool ModernChat
    {
        get => modernChat;
        set
        {
            if (SetProperty(ref modernChat, value))
            {
                Settings.SetBoolean(ConfigKeys.ModernChat, value);
                RefreshCurrentBuffer();
            }
        }
    }

    public bool ReducedPadding
    {
        get => reducedPadding;
        set
        {
            if (SetProperty(ref reducedPadding, value))
            {
                Settings.SetBoolean(ConfigKeys.ReducedPadding, value);
                RefreshCurrentBuffer();
            }
        }
    }

    public int ChatFontSize
    {
        get => chatFontSize;
        set
        {
            value = Math.Clamp(value, 10, 24);
            if (SetProperty(ref chatFontSize, value))
            {
                Settings.SetInt(ConfigKeys.FontSize, value);
            }
        }
    }

    public string ChatFontFamily
    {
        get => chatFontFamily;
        set
        {
            value = string.IsNullOrWhiteSpace(value) ? "Inter" : value.Trim();
            if (SetProperty(ref chatFontFamily, value))
            {
                Settings.SetString(ConfigKeys.FontFamily, value);
            }
        }
    }

    public async Task LoadAsync()
    {
        await ServerStore.LoadAsync();
    }

    public async Task ConnectAsync(WinIrcServer server)
    {
        var existing = sessions.FirstOrDefault(session =>
            string.Equals(session.Server.Name, server.Name, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            SelectedItem = SidebarItems.FirstOrDefault(item => item.Session == existing && item.IsServer);
            return;
        }

        var session = new IrcSession(Clone(server), connectionFactoryHolder.Factory, Settings, new AvaloniaAppDispatcher());
        session.SessionEvent += Session_SessionEvent;
        sessions.Add(session);

        foreach (var channel in session.GetChannelNames())
        {
            AddSidebarItem(session, channel);
        }

        SelectedItem = SidebarItems.FirstOrDefault(item => item.Session == session && item.IsServer);
        await session.ConnectAsync();
    }

    public async Task ConnectAllSavedAsync()
    {
        if (ServerStore.Servers.Count == 0)
        {
            await ServerStore.LoadAsync();
        }

        foreach (var server in ServerStore.Servers.ToList())
        {
            await ConnectAsync(server);
        }
    }

    public Task ConnectSupportAsync()
    {
        return ConnectAsync(new WinIrcServer
        {
            Name = "WinIRC Support (Libera)",
            Hostname = "irc.libera.chat",
            Port = 6697,
            Ssl = true,
            Channels = "#WinIRC",
            Username = string.IsNullOrWhiteSpace(DefaultUsername)
                ? "winircuser"
                : DefaultUsername
        });
    }

    public async Task RequestChannelListAsync()
    {
        var session = SelectedItem?.Session ?? sessions.FirstOrDefault();
        if (session == null)
        {
            StatusText = "Connect to a server before requesting the channel list.";
            return;
        }

        channelListSession = session;
        channelListEntries.Clear();
        ChannelListItems.Clear();
        ChannelListStatus = "Waiting for the server channel list...";
        StatusText = $"{session.Server.Name}: requesting channel list";
        await session.RequestChannelListAsync();
    }

    public async Task JoinListedChannelAsync(ChannelListEntryViewModel? channel)
    {
        var session = channelListSession ?? SelectedItem?.Session ?? sessions.FirstOrDefault();
        if (session == null || channel == null)
        {
            return;
        }

        await session.SendAsync(IrcSession.ServerBufferName, "/join " + channel.Channel);
    }

    public async Task UploadFileToInputAsync(Stream stream, string fileName)
    {
        await UploadContentToInputAsync(stream, fileName, UploadFileUrl, $"Uploading {fileName}...");
    }

    public async Task UploadTextToInputAsync(string text)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await UploadContentToInputAsync(stream, "paste.txt", PasteTextUrl, "Uploading paste...");
    }

    public async Task UploadImageToInputAsync(Stream stream)
    {
        await UploadContentToInputAsync(stream, "upload.png", PasteImageUrl, "Uploading image...");
    }

    public void AppendPastedTextToInput(string text)
    {
        AppendToInput(text.ReplaceLineEndings(" ").Trim());
    }

    public void SetLogsFolderPath(string path)
    {
        LogsFolderPath = path;
    }

    public void AppendToInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        InputText = string.IsNullOrWhiteSpace(InputText)
            ? text
            : InputText.TrimEnd() + " " + text;
    }

    public async Task DisconnectSelectedAsync()
    {
        var session = SelectedItem?.Session;
        if (session == null)
        {
            return;
        }

        await session.DisconnectAsync();
        RemoveSession(session);
    }

    public async Task ReconnectSelectedAsync()
    {
        var session = SelectedItem?.Session;
        if (session == null)
        {
            return;
        }

        await session.DisconnectAsync();
        await session.ConnectAsync();
    }

    public async Task JoinChannelAsync(string channel)
    {
        var session = SelectedItem?.Session ?? sessions.FirstOrDefault();
        if (session == null || string.IsNullOrWhiteSpace(channel))
        {
            return;
        }

        if (!channel.StartsWith('#') && !channel.StartsWith('&'))
        {
            channel = "#" + channel;
        }

        await session.SendAsync(IrcSession.ServerBufferName, "/join " + channel);
    }

    public async Task PartSelectedChannelAsync()
    {
        if (SelectedItem == null || SelectedItem.IsServer)
        {
            return;
        }

        await SelectedItem.Session.SendAsync(SelectedItem.Channel, "/part " + SelectedItem.Channel);
    }

    public void IncreaseFontSize()
    {
        ChatFontSize++;
    }

    public void DecreaseFontSize()
    {
        ChatFontSize--;
    }

    public void MentionUser(string user)
    {
        if (SelectedItem == null || SelectedItem.IsServer || string.IsNullOrWhiteSpace(user))
        {
            return;
        }

        InputText = string.IsNullOrWhiteSpace(InputText)
            ? $"{user}: "
            : InputText.TrimEnd() + " " + user + " ";
    }

    public Task QueryUserAsync(string user)
    {
        return SendUserCommandAsync("/query", user);
    }

    public Task WhoisUserAsync(string user)
    {
        return SendUserCommandAsync("/whois", user);
    }

    public Task OpUserAsync(string user)
    {
        return SendUserCommandAsync("/op", user);
    }

    public Task DeopUserAsync(string user)
    {
        return SendUserCommandAsync("/deop", user);
    }

    public Task VoiceUserAsync(string user)
    {
        return SendUserCommandAsync("/voice", user);
    }

    public Task DevoiceUserAsync(string user)
    {
        return SendUserCommandAsync("/devoice", user);
    }

    public Task MuteUserAsync(string user)
    {
        return SendUserCommandAsync("/mute", user);
    }

    public Task UnmuteUserAsync(string user)
    {
        return SendUserCommandAsync("/unmute", user);
    }

    public Task KickUserAsync(string user)
    {
        return SendUserCommandAsync("/kick", user);
    }

    public async Task BanUserAsync(string user)
    {
        await SendUserCommandAsync("/ban", user);
        await SendUserCommandAsync("/kick", user);
    }

    public async Task ActivateUserAsync(string user)
    {
        switch (UserListClickIndex)
        {
            case 1:
                await QueryUserAsync(user);
                break;

            case 2:
                break;

            default:
                MentionUser(user);
                break;
        }
    }

    public async Task SendCurrentAsync()
    {
        var item = SelectedItem;
        if (item == null || string.IsNullOrWhiteSpace(InputText))
        {
            return;
        }

        var message = InputText;
        InputText = "";
        AddInputHistory(message);
        await item.Session.SendAsync(item.Channel, message);
    }

    public bool TryNavigateInputHistory(bool previous)
    {
        if (inputHistory.Count == 0)
        {
            return false;
        }

        ResetTabCompletion();
        if (previous)
        {
            if (inputHistoryIndex == -1)
            {
                pendingHistoryInput = InputText;
                inputHistoryIndex = inputHistory.Count - 1;
            }
            else
            {
                inputHistoryIndex = Math.Max(0, inputHistoryIndex - 1);
            }

            InputText = inputHistory[inputHistoryIndex];
            return true;
        }

        if (inputHistoryIndex == -1)
        {
            return false;
        }

        inputHistoryIndex++;
        if (inputHistoryIndex >= inputHistory.Count)
        {
            inputHistoryIndex = -1;
            InputText = pendingHistoryInput;
        }
        else
        {
            InputText = inputHistory[inputHistoryIndex];
        }

        return true;
    }

    public int? TryApplyTabCompletion(int caretIndex)
    {
        var item = SelectedItem;
        if (item == null || string.IsNullOrWhiteSpace(InputText))
        {
            ResetTabCompletion();
            return null;
        }

        caretIndex = Math.Clamp(caretIndex, 0, InputText.Length);

        if (tabCompletionOptions.Count > 0 &&
            caretIndex == tabCompletionLastCaret &&
            string.Equals(InputText, tabCompletionLastText, StringComparison.Ordinal))
        {
            tabCompletionIndex = (tabCompletionIndex + 1) % tabCompletionOptions.Count;
        }
        else
        {
            var word = GetCurrentInputWord(InputText, caretIndex);
            if (string.IsNullOrWhiteSpace(word.Text))
            {
                ResetTabCompletion();
                return null;
            }

            tabCompletionOptions = item.Session
                .GetTabCompletions(item.Channel, InputText, caretIndex)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (tabCompletionOptions.Count == 0)
            {
                ResetTabCompletion();
                return null;
            }

            tabCompletionOriginalText = InputText;
            tabCompletionOriginalStart = word.Start;
            tabCompletionOriginalEnd = caretIndex;
            tabCompletionIndex = 0;
        }

        var replacement = FormatCompletion(
            tabCompletionOptions[tabCompletionIndex],
            tabCompletionOriginalText,
            tabCompletionOriginalStart);
        InputText = tabCompletionOriginalText[..tabCompletionOriginalStart] +
            replacement +
            tabCompletionOriginalText[tabCompletionOriginalEnd..];

        tabCompletionLastCaret = tabCompletionOriginalStart + replacement.Length;
        tabCompletionLastText = InputText;
        return tabCompletionLastCaret;
    }

    public void ResetTabCompletion()
    {
        tabCompletionOptions = Array.Empty<string>();
        tabCompletionOriginalText = "";
        tabCompletionLastText = "";
        tabCompletionOriginalStart = 0;
        tabCompletionOriginalEnd = 0;
        tabCompletionLastCaret = -1;
        tabCompletionIndex = -1;
    }

    private static (string Text, int Start) GetCurrentInputWord(string input, int caretIndex)
    {
        var start = caretIndex;
        while (start > 0 && !char.IsWhiteSpace(input[start - 1]))
        {
            start--;
        }

        return (input[start..caretIndex], start);
    }

    private static string FormatCompletion(string completion, string originalText, int start)
    {
        if (originalText.StartsWith("/", StringComparison.Ordinal) || completion.StartsWith("/", StringComparison.Ordinal))
        {
            return completion + " ";
        }

        return start == 0 ? completion + ": " : completion + " ";
    }

    private async Task SendUserCommandAsync(string command, string user)
    {
        if (SelectedItem == null || SelectedItem.IsServer || string.IsNullOrWhiteSpace(user))
        {
            return;
        }

        user = CleanUser(user);
        if (string.IsNullOrWhiteSpace(user))
        {
            return;
        }

        await SelectedItem.Session.SendAsync(SelectedItem.Channel, $"{command} {user}");
    }

    private async Task UploadContentToInputAsync(Stream stream, string fileName, string uploadUrl, string statusText)
    {
        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            StatusText = "Upload URL must be an absolute HTTP or HTTPS URL.";
            return;
        }

        StatusText = statusText;

        try
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(stream);
            form.Add(fileContent, "file", fileName);

            using var response = await UploadClient.PostAsync(uri, form);
            var responseText = (await response.Content.ReadAsStringAsync()).Trim();

            if (!response.IsSuccessStatusCode)
            {
                StatusText = $"Upload failed: {(int)response.StatusCode} {response.ReasonPhrase}";
                return;
            }

            AppendToInput(responseText);
            StatusText = "Upload complete.";
        }
        catch (Exception ex)
        {
            StatusText = "Upload failed: " + ex.Message;
        }
    }

    private void Session_SessionEvent(object? sender, IrcSessionEventArgs e)
    {
        if (sender is not IrcSession session)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplySessionEvent(session, e);
        }
        else
        {
            Dispatcher.UIThread.Post(() => ApplySessionEvent(session, e));
        }
    }

    private void ApplySessionEvent(IrcSession session, IrcSessionEventArgs e)
    {
        switch (e.Kind)
        {
            case IrcSessionEventKind.StatusChanged:
                StatusText = $"{session.Server.Name}: {e.Text}";
                RefreshSidebarStates(session);
                break;

            case IrcSessionEventKind.ChannelAdded:
                if (!string.IsNullOrWhiteSpace(e.Channel))
                {
                    AddSidebarItem(session, e.Channel);
                    SelectJoinedChannel(session, e.Channel);
                }
                break;

            case IrcSessionEventKind.ChannelRemoved:
                RemoveSidebarItem(session, e.Channel);
                if (SelectedItem?.Session == session && SameChannel(SelectedItem.Channel, e.Channel))
                {
                    SelectedItem = SidebarItems.FirstOrDefault(item => item.Session == session && item.IsServer);
                }
                break;

            case IrcSessionEventKind.MessageAdded:
                if (SelectedItem?.Session == session && SameChannel(SelectedItem.Channel, e.Channel) && e.Message != null && ShouldDisplay(e.Message))
                {
                    pendingCurrentMessages.Add(new PendingMessage(session, e.Channel ?? IrcSession.ServerBufferName, e.Message));
                    ScheduleMessageFlush();
                }
                ScheduleSidebarRefresh(session);
                break;

            case IrcSessionEventKind.UsersUpdated:
                if (SelectedItem?.Session == session && SameChannel(SelectedItem.Channel, e.Channel) && e.Users != null)
                {
                    ReplaceUsers(e.Users);
                }
                break;

            case IrcSessionEventKind.TopicChanged:
                if (SelectedItem?.Session == session && SameChannel(SelectedItem.Channel, e.Channel))
                {
                    CurrentTopic = e.Text ?? "";
                }
                break;

            case IrcSessionEventKind.ChannelListReceived:
                if (e.ChannelList != null)
                {
                    channelListSession = session;
                    channelListEntries.Clear();
                    channelListEntries.AddRange(e.ChannelList);
                    RefreshChannelList();
                    StatusText = $"{session.Server.Name}: received {channelListEntries.Count} channels";
                }
                break;

            case IrcSessionEventKind.Disconnected:
                StatusText = e.Exception == null
                    ? $"{session.Server.Name}: disconnected"
                    : $"{session.Server.Name}: disconnected - {e.Exception.Message}";
                break;
        }
    }

    private void RefreshCurrentBuffer()
    {
        if (refreshingCurrentBuffer)
        {
            return;
        }

        refreshingCurrentBuffer = true;
        pendingCurrentMessages.Clear();
        CurrentMessages.Clear();
        CurrentUsers.Clear();

        if (SelectedItem == null)
        {
            CurrentTitle = "WinIRC";
            CurrentTopic = "Connect to a server to start chatting.";
            refreshingCurrentBuffer = false;
            return;
        }

        var session = SelectedItem.Session;
        session.SelectChannel(SelectedItem.Channel);
        CurrentTitle = SelectedItem.IsServer
            ? session.Server.Name
            : $"{session.Server.Name} / {SelectedItem.Channel}";
        CurrentTopic = SelectedItem.IsServer ? "Server log" : session.GetTopic(SelectedItem.Channel);

        CurrentMessages.AddRange(CreateMessageViewModels(session.GetMessages(SelectedItem.Channel)
            .Where(ShouldDisplay)));
        TrimCurrentMessages();

        ReplaceUsers(session.GetUsers(SelectedItem.Channel));
        RefreshSidebarStates(session);
        refreshingCurrentBuffer = false;
    }

    private void ReplaceUsers(IReadOnlyList<string> users)
    {
        CurrentUsers.Clear();

        foreach (var user in users)
        {
            CurrentUsers.Add(user);
        }
    }

    private void AddSidebarItem(IrcSession session, string channel)
    {
        if (SidebarItems.Any(item => item.Session == session && string.Equals(item.Channel, channel, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var item = new SidebarItemViewModel(session, channel)
        {
            UnreadCount = session.GetUnreadCount(channel),
            HasMentions = session.HasMentions(channel)
        };

        SidebarItems.Add(item);
    }

    private void SelectJoinedChannel(IrcSession session, string channel)
    {
        if (!SwitchOnJoin || channel == IrcSession.ServerBufferName)
        {
            return;
        }

        if (SelectedItem?.Session != session)
        {
            return;
        }

        var item = SidebarItems.FirstOrDefault(existing =>
            existing.Session == session &&
            string.Equals(existing.Channel, channel, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            SelectedItem = item;
        }
    }

    private void RemoveSidebarItem(IrcSession session, string? channel)
    {
        var item = SidebarItems.FirstOrDefault(existing =>
            existing.Session == session &&
            string.Equals(existing.Channel, channel, StringComparison.OrdinalIgnoreCase));

        if (item != null)
        {
            SidebarItems.Remove(item);
        }
    }

    private void RemoveSession(IrcSession session)
    {
        sessions.Remove(session);
        session.Dispose();
        pendingCurrentMessages.RemoveAll(message => message.Session == session);
        pendingSidebarRefreshes.Remove(session);

        foreach (var item in SidebarItems.Where(item => item.Session == session).ToList())
        {
            SidebarItems.Remove(item);
        }

        if (SelectedItem?.Session == session)
        {
            SelectedItem = SidebarItems.FirstOrDefault();
        }
    }

    private void RefreshSidebarStates(IrcSession session)
    {
        foreach (var item in SidebarItems.Where(item => item.Session == session))
        {
            item.UnreadCount = item == SelectedItem ? 0 : session.GetUnreadCount(item.Channel);
            item.HasMentions = item != SelectedItem && session.HasMentions(item.Channel);
        }
    }

    private void ScheduleMessageFlush()
    {
        if (Interlocked.Exchange(ref messageFlushScheduled, 1) == 1)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(33);
            Dispatcher.UIThread.Post(FlushPendingMessages);
        });
    }

    private void FlushPendingMessages()
    {
        Interlocked.Exchange(ref messageFlushScheduled, 0);

        if (pendingCurrentMessages.Count == 0)
        {
            return;
        }

        var item = SelectedItem;
        if (item == null)
        {
            pendingCurrentMessages.Clear();
            return;
        }

        var messages = pendingCurrentMessages
            .Where(message => message.Session == item.Session && string.Equals(message.Channel, item.Channel, StringComparison.OrdinalIgnoreCase))
            .Select(message => message.Message)
            .ToList();

        pendingCurrentMessages.Clear();

        if (messages.Count == 0)
        {
            return;
        }

        CurrentMessages.AddRange(CreateMessageViewModels(messages, CurrentMessages.LastOrDefault()));
        TrimCurrentMessages();
    }

    private List<MessageViewModel> CreateMessageViewModels(IEnumerable<Message> messages, MessageViewModel? previous = null)
    {
        var viewModels = new List<MessageViewModel>();
        var last = previous;

        foreach (var message in messages)
        {
            var viewModel = new MessageViewModel(message, last, ModernChat, DarkTheme, ReducedPadding);
            viewModels.Add(viewModel);
            last = viewModel;
        }

        return viewModels;
    }

    private void ScheduleSidebarRefresh(IrcSession session)
    {
        pendingSidebarRefreshes.Add(session);

        if (Interlocked.Exchange(ref sidebarRefreshScheduled, 1) == 1)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(75);
            Dispatcher.UIThread.Post(FlushSidebarRefreshes);
        });
    }

    private void FlushSidebarRefreshes()
    {
        Interlocked.Exchange(ref sidebarRefreshScheduled, 0);

        foreach (var session in pendingSidebarRefreshes.ToList())
        {
            RefreshSidebarStates(session);
        }

        pendingSidebarRefreshes.Clear();
    }

    private void TrimCurrentMessages()
    {
        if (CurrentMessages.Count > 1000)
        {
            CurrentMessages.RemoveRange(0, CurrentMessages.Count - 1000);
        }
    }

    private static bool SameChannel(string left, string? right)
    {
        return string.Equals(left, right ?? IrcSession.ServerBufferName, StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldDisplay(Message message)
    {
        return !IgnoreJoinLeave || message.Type != MessageType.JoinPart;
    }

    private void RefreshChannelList()
    {
        var query = ChannelListSearch.Trim();
        var matches = string.IsNullOrWhiteSpace(query)
            ? channelListEntries
            : channelListEntries.Where(entry =>
                entry.Channel.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                entry.Topic.Contains(query, StringComparison.OrdinalIgnoreCase));

        var viewItems = matches
            .OrderByDescending(entry => entry.Users)
            .ThenBy(entry => entry.Channel, StringComparer.OrdinalIgnoreCase)
            .Take(500)
            .Select(entry => new ChannelListEntryViewModel(entry))
            .ToList();

        ChannelListItems.Clear();
        ChannelListItems.AddRange(viewItems);

        ChannelListStatus = channelListEntries.Count == 0
            ? "Request a channel list from the current server."
            : $"Showing {viewItems.Count} of {channelListEntries.Count} channels.";
    }

    private void AddInputHistory(string message)
    {
        message = message.Trim();
        if (message.Length == 0)
        {
            return;
        }

        if (inputHistory.Count == 0 || inputHistory[^1] != message)
        {
            inputHistory.Add(message);
        }

        while (inputHistory.Count > 200)
        {
            inputHistory.RemoveAt(0);
        }

        inputHistoryIndex = -1;
        pendingHistoryInput = "";
    }

    private static string CleanUser(string user)
    {
        return user.Trim().TrimStart('@', '+', '%', '&', '~');
    }

    private static void ApplyTheme(bool useDarkTheme)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = useDarkTheme ? ThemeVariant.Dark : ThemeVariant.Default;
        }
    }

    private static WinIrcServer Clone(WinIrcServer server)
    {
        return new WinIrcServer
        {
            Name = server.Name,
            Hostname = server.Hostname,
            Port = server.Port,
            Ssl = server.Ssl,
            WebSocket = server.WebSocket,
            Username = server.Username,
            Password = server.Password,
            NickservPassword = server.NickservPassword,
            Channels = server.Channels
        };
    }

    private sealed class IrcConnectionFactoryHolder
    {
        public IIrcConnectionFactory Factory { get; } = new DefaultIrcConnectionFactory();
    }

    private sealed record PendingMessage(IrcSession Session, string Channel, Message Message);
}
