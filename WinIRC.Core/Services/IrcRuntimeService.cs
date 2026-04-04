using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IrcClientCore;
using WinIRC.Core.Interfaces;
using WinIRC.Core.Models;

namespace WinIRC.Core.Services;

public sealed class IrcRuntimeService : IIrcRuntimeService, IDisposable
{
    private const string LastReadKeyPrefix = "runtime.lastread.";

    private readonly SynchronizationContext _synchronizationContext;
    private readonly IWorkspaceSessionService _workspaceSessions;
    private readonly ISettingsStore _settingsStore;
    private readonly Dictionary<string, ServerRuntime> _servers = new(StringComparer.OrdinalIgnoreCase);

    public IrcRuntimeService(IWorkspaceSessionService workspaceSessions, ISettingsStore settingsStore)
    {
        _workspaceSessions = workspaceSessions;
        _settingsStore = settingsStore;
        _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public IrcRuntimeSessionState GetSession(SavedServer server, string channelName)
    {
        return GetOrCreateServer(server).GetSession(NormalizeChannel(channelName));
    }

    public IrcRuntimeSessionState? TryGetExistingSession(SavedServer server, string channelName)
    {
        return _servers.TryGetValue(server.Name, out var runtime)
            ? runtime.TryGetExistingSession(NormalizeChannel(channelName))
            : null;
    }

    public IrcRuntimeTargetInfo? TryGetTargetInfo(SavedServer server, string channelName)
    {
        return _servers.TryGetValue(server.Name, out var runtime)
            ? runtime.TryGetTargetInfo(NormalizeChannel(channelName))
            : null;
    }

    public Task EnsureConnectedAsync(SavedServer server)
    {
        GetOrCreateServer(server).EnsureConnected();
        return Task.CompletedTask;
    }

    public Task OpenChannelAsync(SavedServer server, string channelName)
    {
        return GetOrCreateServer(server).OpenChannelAsync(NormalizeChannel(channelName));
    }

    public Task SendAsync(SavedServer server, string channelName, string text)
    {
        return GetOrCreateServer(server).SendAsync(NormalizeChannel(channelName), text);
    }

    public void Dispose()
    {
        foreach (var runtime in _servers.Values)
        {
            runtime.Dispose();
        }

        _servers.Clear();
    }

    private ServerRuntime GetOrCreateServer(SavedServer server)
    {
        if (_servers.TryGetValue(server.Name, out var existing))
        {
            existing.UpdateServer(server);
            return existing;
        }

        var created = new ServerRuntime(server, _workspaceSessions, _settingsStore, _synchronizationContext);
        _servers[server.Name] = created;
        return created;
    }

    private static string NormalizeChannel(string channelName)
    {
        if (string.IsNullOrWhiteSpace(channelName) || string.Equals(channelName, "Server", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return channelName.Trim();
    }

    private sealed class ServerRuntime : IDisposable
    {
        private readonly IWorkspaceSessionService _workspaceSessions;
        private readonly ISettingsStore _settingsStore;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly RuntimeIrcClient _client;
        private readonly Timer _refreshTimer;
        private readonly Dictionary<string, SessionBinding> _bindings = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _connectSync = new();
        private SavedServer _server;
        private bool _connectingStarted;

        public ServerRuntime(
            SavedServer server,
            IWorkspaceSessionService workspaceSessions,
            ISettingsStore settingsStore,
            SynchronizationContext synchronizationContext)
        {
            _server = CloneServer(server);
            _workspaceSessions = workspaceSessions;
            _settingsStore = settingsStore;
            _synchronizationContext = synchronizationContext;
            _client = new RuntimeIrcClient(CreateRuntimeServer(_server));
            _client.Initialise();
            _client.HandleDisconnect = _ =>
            {
                _connectingStarted = false;
                UpdateRefreshTimer();
                RefreshAllStates();
            };

            if (_client.ChannelList is INotifyCollectionChanged channels)
            {
                channels.CollectionChanged += ChannelList_CollectionChanged;
            }

            _refreshTimer = new Timer(_ => RefreshAllStates(), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void UpdateServer(SavedServer server)
        {
            _server = CloneServer(server);
        }

        public IrcRuntimeSessionState GetSession(string channelName)
        {
            var key = NormalizeKey(channelName);
            var binding = GetOrCreateBinding(key);

            EnsureConnected();
            BindSession(binding);
            RefreshSession(binding);

            if (binding.State.Messages.Count == 0)
            {
                binding.State.ReplaceMessages(new[]
                {
                    CreateStatusMessage(
                        key.Length == 0
                            ? $"Connecting to {_server.DisplayEndpoint}..."
                            : $"Waiting for live buffer for {key} on {_server.Name}...")
                });
            }

            return binding.State;
        }

        public IrcRuntimeSessionState? TryGetExistingSession(string channelName)
        {
            var key = NormalizeKey(channelName);
            return _bindings.TryGetValue(key, out var binding)
                ? binding.State
                : null;
        }

        public IrcRuntimeTargetInfo? TryGetTargetInfo(string channelName)
        {
            var existing = TryGetExistingSession(channelName);
            if (existing != null)
            {
                return new IrcRuntimeTargetInfo
                {
                    IsConnected = existing.IsConnected,
                    ConnectionState = existing.ConnectionState,
                    ParticipantCount = existing.Participants.Count,
                    Topic = existing.Topic
                };
            }

            var liveChannel = TryGetLiveChannelSafe(channelName);
            if (liveChannel == null)
            {
                return null;
            }

            return new IrcRuntimeTargetInfo
            {
                IsConnected = _client.IsConnected,
                ConnectionState = _client.IsConnected
                    ? _client.IsAuthed
                        ? "Connected"
                        : "Authenticating..."
                    : _client.IsConnecting
                        ? "Connecting..."
                        : "Disconnected",
                ParticipantCount = SnapshotParticipants(liveChannel.Store?.RawUsers, string.Empty, string.Empty)?.Length ?? 0,
                Topic = liveChannel.Store?.Topic ?? string.Empty
            };
        }

        public void EnsureConnected()
        {
            if (_connectingStarted || _client.IsConnected || _client.IsConnecting)
            {
                RefreshAllStates();
                return;
            }

            lock (_connectSync)
            {
                if (_connectingStarted || _client.IsConnected || _client.IsConnecting)
                {
                    RefreshAllStates();
                    return;
                }

                _connectingStarted = true;
            }

            UpdateRefreshTimer();
            RefreshAllStates();

            _ = Task.Run(() =>
            {
                try
                {
                    _client.Connect();
                }
                finally
                {
                    RefreshAllStates();
                }
            });
        }

        public async Task OpenChannelAsync(string channelName)
        {
            if (channelName.Length == 0)
            {
                EnsureConnected();
                return;
            }

            EnsureConnected();
            _workspaceSessions.AddTransientTarget(_server.Name, channelName, !channelName.StartsWith("#", StringComparison.Ordinal));

            if (!channelName.StartsWith("#", StringComparison.Ordinal))
            {
                GetSession(channelName);
                return;
            }

            if (_client.ChannelList.Contains(channelName))
            {
                GetSession(channelName);
                return;
            }

            await WaitForReadyAsync().ConfigureAwait(false);
            await _client.JoinChannel(channelName).ConfigureAwait(false);
            RefreshAllStates();
        }

        public async Task SendAsync(string channelName, string text)
        {
            EnsureConnected();

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (channelName.Length == 0 && !text.StartsWith("/", StringComparison.Ordinal))
            {
                PostToUi(() =>
                {
                    var state = GetSession(string.Empty);
                    state.AddMessage(CreateStatusMessage("Use /join, /query, or another IRC command from the server session."));
                });
                return;
            }

            await WaitForReadyAsync().ConfigureAwait(false);
            _client.CommandManager.HandleCommand(channelName.Length > 0 ? channelName : "Server", text);

            if (text.StartsWith("/part", StringComparison.OrdinalIgnoreCase) && channelName.Length > 0)
            {
                _workspaceSessions.RemoveTransientTarget(_server.Name, channelName);
            }

            RefreshAllStates();
        }

        public void Dispose()
        {
            _refreshTimer.Dispose();

            foreach (var binding in _bindings.Values)
            {
                DetachBinding(binding);
            }

            if (_client.ChannelList is INotifyCollectionChanged channels)
            {
                channels.CollectionChanged -= ChannelList_CollectionChanged;
            }

            if (_client.Connection is IDisposable disposableConnection)
            {
                disposableConnection.Dispose();
            }
        }

        private void ChannelList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var channel in e.NewItems.OfType<Channel>().Where(item => !item.ServerLog))
                {
                    EnsureTrackedSession(channel.Name);
                }

                _workspaceSessions.AddTransientTargets(
                    _server.Name,
                    e.NewItems.OfType<Channel>()
                        .Where(item => !item.ServerLog)
                        .Select(item => new WorkspaceTarget
                        {
                            Name = item.Name,
                            IsDirectMessage = !item.Name.StartsWith("#", StringComparison.Ordinal)
                        }));
            }

            if (e.OldItems != null)
            {
                _workspaceSessions.RemoveTransientTargets(
                    _server.Name,
                    e.OldItems.OfType<Channel>()
                        .Where(item => !item.ServerLog)
                        .Select(item => item.Name));
            }

            RefreshAllStates();
        }

        private async Task WaitForReadyAsync()
        {
            for (var attempt = 0; attempt < 40; attempt++)
            {
                if (_client.IsConnected)
                {
                    return;
                }

                await Task.Delay(250).ConfigureAwait(false);
            }
        }

        private void RefreshAllStates()
        {
            UpdateRefreshTimer();

            foreach (var binding in _bindings.Values)
            {
                BindSession(binding);
                RefreshSession(binding);
            }
        }

        private void RefreshSession(SessionBinding binding)
        {
            PostToUi(() =>
            {
                var state = binding.State;
                var previousNickname = state.Nickname;
                var resolvedNickname = string.IsNullOrWhiteSpace(_client.Nickname) ? _server.Username : (_client.Nickname ?? _server.Username);
                var joinedTargetsSnapshot = SnapshotJoinedTargets();
                var liveChannel = TryGetLiveChannelSafe(state.ChannelName);
                var topic = liveChannel?.Store?.Topic;
                var participantSnapshot = SnapshotParticipants(liveChannel?.Store?.RawUsers, previousNickname, resolvedNickname);

                state.RunBatchUpdate(() =>
                {
                    state.IsConnected = _client.IsConnected;
                    state.Nickname = resolvedNickname;
                    state.Motd = _client.MOTD ?? string.Empty;
                    state.ConnectionState = _client.IsConnected
                        ? _client.IsAuthed
                            ? $"Connected as {state.Nickname} to {_server.DisplayEndpoint}"
                            : $"Authenticating with {_server.DisplayEndpoint}..."
                        : _client.IsConnecting
                            ? $"Connecting to {_server.DisplayEndpoint}..."
                            : $"Disconnected from {_server.DisplayEndpoint}";
                    state.ResetJoinedTargets(joinedTargetsSnapshot);
                    state.Topic = state.ChannelName.Length == 0
                        ? BuildServerTopic(state)
                        : string.IsNullOrWhiteSpace(topic)
                            ? $"Live session for {state.ChannelName} on {_server.Name}"
                            : topic ?? string.Empty;

                    if (participantSnapshot != null)
                    {
                        state.ResetParticipants(participantSnapshot);
                    }
                    else
                    {
                        state.ResetParticipants(Array.Empty<string>());
                    }
                });
            });
        }

        private void UpdateRefreshTimer()
        {
            var shouldPoll = _connectingStarted && (!_client.IsConnected || !_client.IsAuthed);
            _refreshTimer.Change(
                shouldPoll ? TimeSpan.Zero : Timeout.InfiniteTimeSpan,
                shouldPoll ? TimeSpan.FromSeconds(1) : Timeout.InfiniteTimeSpan);
        }

        private void BindSession(SessionBinding binding)
        {
            var liveChannel = TryGetLiveChannel(binding.State.ChannelName);
            var liveMessages = (liveChannel?.Buffers as RuntimeBuffer)?.Collection;
            if (!ReferenceEquals(binding.MessageSource, liveMessages))
            {
                if (binding.MessageSource != null && binding.MessageHandler != null)
                {
                    binding.MessageSource.CollectionChanged -= binding.MessageHandler;
                }

                binding.MessageSource = liveMessages;
                if (liveMessages != null)
                {
                    binding.MessageHandler = (_, args) => HandleMessageCollectionChanged(binding, args);
                    liveMessages.CollectionChanged += binding.MessageHandler;
                    PostToUi(() => binding.State.ReplaceMessages(liveMessages.Select(MapMessage)));
                }
                else
                {
                    binding.MessageHandler = null;
                }
            }

            var liveParticipants = liveChannel?.Store?.RawUsers;
            if (!ReferenceEquals(binding.ParticipantSource, liveParticipants))
            {
                if (binding.ParticipantSource != null && binding.ParticipantHandler != null)
                {
                    binding.ParticipantSource.CollectionChanged -= binding.ParticipantHandler;
                }

                binding.ParticipantSource = liveParticipants;
                if (liveParticipants != null)
                {
                    binding.ParticipantHandler = (_, _) => RefreshSession(binding);
                    liveParticipants.CollectionChanged += binding.ParticipantHandler;
                    RefreshSession(binding);
                }
                else
                {
                    binding.ParticipantHandler = null;
                }
            }
        }

        private void HandleMessageCollectionChanged(SessionBinding binding, NotifyCollectionChangedEventArgs args)
        {
            PostToUi(() =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset && binding.MessageSource != null)
                {
                    binding.State.ReplaceMessages(binding.MessageSource.Select(MapMessage));
                    RefreshSession(binding);
                    return;
                }

                var requiresMetadataRefresh = false;

                if (args.OldItems != null)
                {
                    requiresMetadataRefresh = true;
                    for (var index = 0; index < args.OldItems.Count; index++)
                    {
                        binding.State.RemoveMessageAt(args.OldStartingIndex);
                    }
                }

                if (args.NewItems != null)
                {
                    foreach (var message in args.NewItems.OfType<Message>())
                    {
                        var runtimeMessage = MapMessage(message);
                        binding.State.AddMessage(runtimeMessage);
                        binding.State.TrackUnread(runtimeMessage);
                    }
                }

                if (requiresMetadataRefresh)
                {
                    RefreshSession(binding);
                }
            });
        }

        private void DetachBinding(SessionBinding binding)
        {
            if (binding.MessageSource != null && binding.MessageHandler != null)
            {
                binding.MessageSource.CollectionChanged -= binding.MessageHandler;
            }

            if (binding.ParticipantSource != null && binding.ParticipantHandler != null)
            {
                binding.ParticipantSource.CollectionChanged -= binding.ParticipantHandler;
            }
        }

        private Channel? TryGetLiveChannel(string channelName)
        {
            if (channelName.Length == 0)
            {
                return _client.ChannelList.ServerLog;
            }

            return _client.ChannelList.Contains(channelName)
                ? _client.ChannelList[channelName]
                : null;
        }

        private void EnsureTrackedSession(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                return;
            }

            var key = NormalizeKey(channelName);
            var binding = GetOrCreateBinding(key);
            BindSession(binding);
            RefreshSession(binding);
        }

        private SessionBinding GetOrCreateBinding(string key)
        {
            if (_bindings.TryGetValue(key, out var binding))
            {
                return binding;
            }

            var state = new IrcRuntimeSessionState
            {
                ServerName = _server.Name,
                ChannelName = key
            };
            state.ConfigureReadTracking(
                LoadLastReadTimestamp(key),
                timestamp => PersistLastReadTimestamp(key, timestamp));
            binding = new SessionBinding(state);
            _bindings[key] = binding;
            return binding;
        }

        private DateTimeOffset? LoadLastReadTimestamp(string channelName)
        {
            var raw = _settingsStore.GetString(BuildLastReadKey(channelName), string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return DateTimeOffset.TryParse(raw, out var parsed)
                ? parsed
                : null;
        }

        private void PersistLastReadTimestamp(string channelName, DateTimeOffset? timestamp)
        {
            if (!timestamp.HasValue)
            {
                return;
            }

            _ = _settingsStore.SetStringAsync(
                BuildLastReadKey(channelName),
                timestamp.Value.ToString("O"));
        }

        private string BuildLastReadKey(string channelName)
        {
            return LastReadKeyPrefix + _server.Name + "|" + channelName;
        }

        private Channel? TryGetLiveChannelSafe(string channelName)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    return TryGetLiveChannel(channelName);
                }
                catch (InvalidOperationException) when (attempt < 2)
                {
                    Thread.Yield();
                }
            }

            return null;
        }

        private string[] SnapshotJoinedTargets()
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    return _client.ChannelList
                        .Where(channel => !channel.ServerLog)
                        .Select(channel => channel.Name ?? string.Empty)
                        .ToArray();
                }
                catch (InvalidOperationException) when (attempt < 2)
                {
                    Thread.Yield();
                }
            }

            return Array.Empty<string>();
        }

        private static string[]? SnapshotParticipants(
            ObservableCollection<string>? participants,
            string previousNickname,
            string currentNickname)
        {
            if (participants == null)
            {
                return null;
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    return ProjectParticipants(participants.ToArray(), previousNickname, currentNickname).ToArray();
                }
                catch (InvalidOperationException) when (attempt < 2)
                {
                    Thread.Yield();
                }
            }

            return Array.Empty<string>();
        }

        private void PostToUi(Action action)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                action();
                return;
            }

            _synchronizationContext.Post(_ => action(), null);
        }

        private static SavedServer CloneServer(SavedServer server)
        {
            return new SavedServer
            {
                Name = server.Name,
                Hostname = server.Hostname,
                Port = server.Port,
                Ssl = server.Ssl,
                UseWebSocket = server.UseWebSocket,
                Invalid = server.Invalid,
                Username = server.Username,
                Password = server.Password,
                NickservPassword = server.NickservPassword,
                Channels = server.Channels
            };
        }

        private static RuntimeServerDefinition CreateRuntimeServer(SavedServer server)
        {
            return new RuntimeServerDefinition
            {
                Name = server.Name,
                Hostname = server.Hostname,
                Port = server.Port,
                Ssl = server.Ssl,
                IgnoreCertErrors = false,
                ShouldReconnect = false,
                Username = server.Username,
                Password = server.Password,
                NickservPassword = server.NickservPassword,
                Channels = server.Channels,
                UseWebSocket = server.UseWebSocket
            };
        }

        private static string NormalizeKey(string channelName)
        {
            return string.IsNullOrWhiteSpace(channelName) ? string.Empty : channelName.Trim();
        }

        private static IEnumerable<string> ProjectParticipants(
            IEnumerable<string> participants,
            string previousNickname,
            string currentNickname)
        {
            if (string.IsNullOrWhiteSpace(currentNickname))
            {
                return participants;
            }

            var canRewriteNick = !string.IsNullOrWhiteSpace(previousNickname)
                && !string.Equals(previousNickname, currentNickname, StringComparison.OrdinalIgnoreCase);

            return participants.Select(participant =>
            {
                if (!canRewriteNick)
                {
                    return participant;
                }

                var prefixLength = participant.TakeWhile(character => "~&@%+".IndexOf(character) >= 0).Count();
                var prefix = prefixLength > 0 ? participant.Substring(0, prefixLength) : string.Empty;
                var bareNickname = prefixLength > 0 ? participant.Substring(prefixLength) : participant;

                return string.Equals(bareNickname, previousNickname, StringComparison.OrdinalIgnoreCase)
                    ? prefix + currentNickname
                    : participant;
            });
        }

        private static string BuildServerTopic(IrcRuntimeSessionState state)
        {
            if (!string.IsNullOrWhiteSpace(state.Motd))
            {
                var firstLine = state.Motd
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .FirstOrDefault(line => line.Length > 0);

                if (!string.IsNullOrWhiteSpace(firstLine))
                {
                    return firstLine;
                }
            }

            return $"Connection status: {state.ConnectionState}";
        }

        private IrcRuntimeMessage MapMessage(Message message)
        {
            var text = message.Text ?? string.Empty;
            var kind = IrcRuntimeMessageKind.Normal;
            if (message.Type == MessageType.Action && text.StartsWith("ACTION ", StringComparison.Ordinal))
            {
                text = "* " + message.User + " " + text.Substring("ACTION ".Length);
                kind = IrcRuntimeMessageKind.Action;
            }
            else if (message.Type == MessageType.JoinPart)
            {
                kind = IrcRuntimeMessageKind.JoinPart;
            }
            else if (message.Type == MessageType.Info || string.IsNullOrWhiteSpace(message.User))
            {
                kind = IrcRuntimeMessageKind.Status;
            }

            var nickname = _client.Nickname ?? _server.Username;
            var isMention = !string.IsNullOrWhiteSpace(nickname)
                && !string.Equals(message.User, nickname, StringComparison.OrdinalIgnoreCase)
                && ContainsMention(text, nickname);

            return new IrcRuntimeMessage
            {
                Author = message.User ?? string.Empty,
                Text = text,
                Timestamp = message.Date == default ? DateTimeOffset.Now : new DateTimeOffset(message.Date),
                IsStatus = message.Type == MessageType.Info || string.IsNullOrWhiteSpace(message.User),
                IsMention = isMention,
                Kind = kind
            };
        }

        private static IrcRuntimeMessage CreateStatusMessage(string text)
        {
            return new IrcRuntimeMessage
            {
                IsStatus = true,
                Kind = IrcRuntimeMessageKind.Status,
                Text = text,
                Timestamp = DateTimeOffset.Now
            };
        }

        private static bool ContainsMention(string text, string nickname)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(nickname))
            {
                return false;
            }

            var comparison = StringComparison.OrdinalIgnoreCase;
            var index = text.IndexOf(nickname, comparison);
            while (index >= 0)
            {
                var before = index == 0 ? '\0' : text[index - 1];
                var afterIndex = index + nickname.Length;
                var after = afterIndex >= text.Length ? '\0' : text[afterIndex];

                if (!IsMentionBoundary(before) || !IsMentionBoundary(after))
                {
                    index = text.IndexOf(nickname, index + nickname.Length, comparison);
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsMentionBoundary(char character)
        {
            return character == '\0' || char.IsWhiteSpace(character) || ",.:;!?()[]{}<>@*_/\\|\"'`".IndexOf(character) >= 0;
        }
    }

    private sealed class SessionBinding
    {
        public SessionBinding(IrcRuntimeSessionState state)
        {
            State = state;
        }

        public IrcRuntimeSessionState State { get; }

        public ObservableCollection<Message>? MessageSource { get; set; }

        public NotifyCollectionChangedEventHandler? MessageHandler { get; set; }

        public ObservableCollection<string>? ParticipantSource { get; set; }

        public NotifyCollectionChangedEventHandler? ParticipantHandler { get; set; }
    }
}
