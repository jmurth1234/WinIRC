using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinIRC.Core;
using WinIRC.Core.Models;
using WinIRC.WinUI.Interfaces;
using WinIRC.WinUI.Models;
using WinIRC.WinUI.Utils;

namespace WinIRC.WinUI.Views;

public sealed partial class ChannelPage : Page, INotifyPropertyChanged, ISessionSidebarSource
{
    private readonly SavedServer? _server;
    private readonly AppBootstrapContext _context;
    private readonly Action<string, string>? _openChannel;
    private readonly Action<string, string>? _openQuery;
    private readonly Action<ChatSessionDescriptor>? _closeSession;
    private IrcRuntimeSessionState? _liveSession;
    private bool _liveSessionLoaded;
    private bool _showTopicBar = true;
    private bool _isActiveView;
    private int _unreadCount;
    private bool _hasUnreadMention;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ChatSessionDescriptor Descriptor { get; }

    public string TitleText => Descriptor.Header;

    public string SubtitleText => Descriptor.Subtitle;

    public string DescriptionText => Descriptor.Description;

    public Visibility SubtitleVisibility => string.IsNullOrWhiteSpace(SubtitleText)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public Visibility DescriptionVisibility => string.IsNullOrWhiteSpace(DescriptionText)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public ObservableCollection<ChannelMessageGroupItem> Messages { get; } = new();

    public ObservableCollection<ChannelFactItem> Facts { get; } = new();

    public ObservableCollection<string> RosterEntries { get; } = new();

    public string TopicText => BuildTopicText();

    public bool SupportsTopicBar => Descriptor.ChannelName.Length > 0;

    public int UnreadCount
    {
        get => _unreadCount;
        private set
        {
            if (_unreadCount == value)
            {
                return;
            }

            _unreadCount = value;
            OnPropertyChanged(nameof(UnreadCount));
        }
    }

    public bool HasUnreadMention
    {
        get => _hasUnreadMention;
        private set
        {
            if (_hasUnreadMention == value)
            {
                return;
            }

            _hasUnreadMention = value;
            OnPropertyChanged(nameof(HasUnreadMention));
        }
    }

    public Visibility TopicVisibility => SupportsTopicBar && _showTopicBar ? Visibility.Visible : Visibility.Collapsed;

    public Visibility HeaderIdentityVisibility => Descriptor.ChannelName.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility HeaderVisibility => HeaderIdentityVisibility == Visibility.Visible
        || TopicVisibility == Visibility.Visible
        || ProtocolActionVisibility == Visibility.Visible
            ? Visibility.Visible
            : Visibility.Collapsed;

    public string FactHeaderText => Descriptor.ChannelName.Length > 0 ? "Channel details" : _server != null ? "Connection details" : "Session details";

    public string RosterHeaderText => BuildRosterHeaderText();

    public string SidebarTitle => TitleText;

    public string SidebarSubtitle => SubtitleText;

    public string SidebarStatusText => TopicText;

    public string SidebarPrimaryHeader => FactHeaderText;

    public ObservableCollection<ChannelFactItem> SidebarPrimaryItems => Facts;

    public string SidebarSecondaryHeader => RosterHeaderText;

    public ObservableCollection<string> SidebarSecondaryItems => RosterEntries;

    public bool CanActivateSidebarSecondaryItem => _server != null;

    public DataTemplate MessageTemplate => (DataTemplate)Resources[
        _context.SettingsStore.GetBoolean(ConfigKeys.ModernChat, true)
            ? "ModernMessageTemplate"
            : "CompactMessageTemplate"];

    public string ComposerPlaceholderText => Descriptor.ChannelName.Length > 0
        ? $"Message {Descriptor.ChannelName}"
        : _server != null
            ? $"Message {_server.Name}"
            : Descriptor.Kind == ChatSessionKind.ProtocolActivation
                ? "Type a note or test command for this activation"
                : "Type a note into the new session host";

    public FontFamily ChatFontFamily => new(_context.SettingsStore.GetString(ConfigKeys.FontFamily, "Cascadia Mono"));

    public double ChatFontSize => _context.SettingsStore.GetInt(ConfigKeys.FontSize, 13);

    public double ChatLineHeight => Math.Round(ChatFontSize + 7);

    public Thickness PagePadding => _context.SettingsStore.GetBoolean(ConfigKeys.ReducedPadding, false)
        ? new Thickness(6, 4, 6, 4)
        : new Thickness(10, 8, 10, 8);

    public double SectionSpacing => _context.SettingsStore.GetBoolean(ConfigKeys.ReducedPadding, false) ? 6 : 8;

    public Thickness HeaderPadding => _context.SettingsStore.GetBoolean(ConfigKeys.ReducedPadding, false)
        ? new Thickness(6, 5, 6, 5)
        : new Thickness(8, 6, 8, 6);

    public Thickness TopicBarPadding => _context.SettingsStore.GetBoolean(ConfigKeys.ReducedPadding, false)
        ? new Thickness(6, 3, 6, 3)
        : new Thickness(8, 4, 8, 4);

    public double TopicBarFontSize => _context.SettingsStore.GetBoolean(ConfigKeys.ReducedPadding, false) ? 11 : 12;

    public double ConversationRowSpacing => _context.SettingsStore.GetBoolean(ConfigKeys.ReducedPadding, false) ? 4 : 6;

    public Thickness ConversationHeaderPadding => _context.SettingsStore.GetBoolean(ConfigKeys.ReducedPadding, false)
        ? new Thickness(8, 6, 8, 0)
        : new Thickness(10, 8, 10, 0);

    public Thickness MessagesPadding => _context.SettingsStore.GetBoolean(ConfigKeys.ReducedPadding, false)
        ? new Thickness(8, 0, 8, 6)
        : new Thickness(10, 0, 10, 8);

    public Thickness ComposerPadding => _context.SettingsStore.GetBoolean(ConfigKeys.ReducedPadding, false)
        ? new Thickness(6)
        : new Thickness(8);

    public Visibility ProtocolActionVisibility => Descriptor.SourceUri != null ? Visibility.Visible : Visibility.Collapsed;

    public ChannelPage(
        ChatSessionDescriptor descriptor,
        SavedServer? server,
        AppBootstrapContext context,
        Action<string, string>? openChannel = null,
        Action<string, string>? openQuery = null,
        Action<ChatSessionDescriptor>? closeSession = null)
    {
        Descriptor = descriptor;
        _server = server;
        _context = context;
        _openChannel = openChannel;
        _openQuery = openQuery;
        _closeSession = closeSession;
        _showTopicBar = _context.SettingsStore.GetBoolean(ConfigKeys.ShowTopicBar, true);

        InitializeComponent();
        ApplyMessageTemplate();

        Loaded += ChannelPage_Loaded;
        Unloaded += ChannelPage_Unloaded;
        BuildSessionSurface();
    }

    private async void ChannelPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_server == null || _liveSessionLoaded)
        {
            return;
        }

        _liveSessionLoaded = true;
        await _context.IrcRuntime.EnsureConnectedAsync(_server);

        if (Descriptor.ChannelName.Length > 0)
        {
            await _context.IrcRuntime.OpenChannelAsync(_server, Descriptor.ChannelName);
        }

        AttachLiveSession(_context.IrcRuntime.GetSession(_server, Descriptor.ChannelName));
        RefreshLiveSurface();
    }

    private void ChannelPage_Unloaded(object sender, RoutedEventArgs e)
    {
        DetachLiveSession();
    }

    private async void OpenUri_Click(object sender, RoutedEventArgs e)
    {
        if (Descriptor.SourceUri == null)
        {
            return;
        }

        await _context.ExternalLauncher.LaunchUriAsync(new Uri(Descriptor.SourceUri));
    }

    private void ComposerTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            CommitDraft();
        }
    }

    private void Send_Click(object sender, RoutedEventArgs e)
    {
        CommitDraft();
    }

    private void AttachNote_Click(object sender, RoutedEventArgs e)
    {
        AddStatusMessage("Attachment and paste handling will land with the picker and upload slice.");
    }

    private void BuildSessionSurface()
    {
        SyncFacts();
        SyncRosterEntries();

        if (_liveSession == null)
        {
            RebuildMessages(BuildInitialMessages());
        }

        NotifyMetadataChanged();
    }

    private IEnumerable<ChannelFactItem> BuildFacts()
    {
        if (_server != null)
        {
            yield return new ChannelFactItem
            {
                Label = "Server",
                Value = _server.Name
            };

            yield return new ChannelFactItem
            {
                Label = "Endpoint",
                Value = _server.DisplayEndpoint
            };
        }

        if (_liveSession != null)
        {
            yield return new ChannelFactItem
            {
                Label = "Status",
                Value = _liveSession.ConnectionState
            };

            if (!string.IsNullOrWhiteSpace(_liveSession.Nickname))
            {
                yield return new ChannelFactItem
                {
                    Label = "Nickname",
                    Value = _liveSession.Nickname
                };
            }

            if (_liveSession.JoinedTargets.Count > 0)
            {
                yield return new ChannelFactItem
                {
                    Label = "Targets",
                    Value = _liveSession.JoinedTargets.Count.ToString()
                };
            }

            if (Descriptor.ChannelName.Length > 0 && _liveSession.Participants.Count > 0)
            {
                yield return new ChannelFactItem
                {
                    Label = "Users",
                    Value = _liveSession.Participants.Count.ToString()
                };
            }
        }
        else if (_server != null)
        {
            yield return new ChannelFactItem
            {
                Label = "Status",
                Value = Descriptor.ChannelName.Length > 0
                    ? "Connecting"
                    : "Opening server session"
            };
        }

        if (Descriptor.SourceUri != null)
        {
            yield return new ChannelFactItem
            {
                Label = "Activated URI",
                Value = Descriptor.SourceUri
            };
        }
    }

    private IEnumerable<string> BuildRosterEntries()
    {
        if (_liveSession != null && Descriptor.ChannelName.Length > 0 && _liveSession.Participants.Count > 0)
        {
            return _liveSession.Participants.ToArray();
        }

        if (_liveSession != null && Descriptor.ChannelName.Length == 0 && _liveSession.JoinedTargets.Count > 0)
        {
            return _liveSession.JoinedTargets.ToArray();
        }

        if (_liveSession != null && Descriptor.ChannelName.Length > 0 && _liveSession.JoinedTargets.Count > 0)
        {
            var relatedLiveTargets = _liveSession.JoinedTargets
                .Where(target => !target.Equals(Descriptor.ChannelName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (relatedLiveTargets.Length > 0)
            {
                return relatedLiveTargets;
            }
        }

        if (_server != null)
        {
            var channels = (_server.Channels ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (Descriptor.ChannelName.Length > 0)
            {
                var relatedTargets = channels
                    .Where(channel => !channel.Equals(Descriptor.ChannelName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (relatedTargets.Length > 0)
                {
                    return relatedTargets;
                }
            }

            if (channels.Length > 0)
            {
                return channels;
            }
        }

        return Array.Empty<string>();
    }

    public void ActivateSidebarSecondaryItem(string item)
    {
        if (_server == null)
        {
            return;
        }

        var target = NormalizeSidebarTarget(item);
        if (target.Length == 0)
        {
            return;
        }

        var isLiveUser = _liveSession?.Participants.Any(participant =>
            string.Equals(NormalizeSidebarTarget(participant), target, StringComparison.OrdinalIgnoreCase)) == true;

        if (isLiveUser)
        {
            _openQuery?.Invoke(_server.Name, target);
            return;
        }

        if (IsChannelTarget(target))
        {
            _openChannel?.Invoke(_server.Name, target);
            return;
        }

        _openQuery?.Invoke(_server.Name, target);
    }

    public void InsertSidebarSecondaryItem(string item)
    {
        var target = NormalizeSidebarTarget(item);
        if (target.Length == 0)
        {
            return;
        }

        if (ComposerTextBox.Text.Length == 0)
        {
            ComposerTextBox.Text = target + ": ";
        }
        else
        {
            ComposerTextBox.Text += target + " ";
        }

        ComposerTextBox.SelectionStart = ComposerTextBox.Text.Length;
        ComposerTextBox.Focus(FocusState.Programmatic);
    }

    private IEnumerable<ChannelMessageItem> BuildInitialMessages()
    {
        var now = DateTimeOffset.Now;
        // Keep seeded transcript content lightweight and user-facing while the live buffer is warming up.
        if (!string.IsNullOrWhiteSpace(DescriptionText))
        {
            yield return StatusMessage(DescriptionText, now.AddMinutes(-1));
        }

        if (_server != null)
        {
            if (Descriptor.ChannelName.Length > 0)
            {
                yield return StatusMessage($"Connecting to {Descriptor.ChannelName}...", now.AddSeconds(-30));
                yield break;
            }

            yield return UserMessage(
                "WinIRC",
                $"Connecting to {_server.Name} ({_server.DisplayEndpoint}).",
                now.AddMinutes(-1));

            if (!string.IsNullOrWhiteSpace(_server.Channels))
            {
                yield return StatusMessage($"Channels: {_server.Channels}", now.AddSeconds(-30));
            }
        }
        else if (Descriptor.SourceUri != null)
        {
            yield return StatusMessage($"Opened from {Descriptor.SourceUri}", now.AddMinutes(-1));
        }
    }

    private string BuildTopicText()
    {
        if (_liveSession != null)
        {
            if (!string.IsNullOrWhiteSpace(_liveSession.Topic))
            {
                return "Topic: " + _liveSession.Topic;
            }

            if (!string.IsNullOrWhiteSpace(_liveSession.Motd))
            {
                return "MOTD: " + _liveSession.Motd;
            }

            return "Status: " + _liveSession.ConnectionState;
        }

        if (_server != null)
        {
            if (Descriptor.ChannelName.Length > 0)
            {
                return $"Connecting to {Descriptor.ChannelName} on {_server.Name}...";
            }

            return $"Connecting to {_server.DisplayEndpoint}...";
        }

        if (Descriptor.SourceUri != null)
        {
            return "Opened from external link";
        }

        return string.Empty;
    }

    private string BuildRosterHeaderText()
    {
        if (_liveSession != null)
        {
            if (Descriptor.ChannelName.Length > 0 && _liveSession.Participants.Count > 0)
            {
                return "Users";
            }

            if (Descriptor.ChannelName.Length == 0 && _liveSession.JoinedTargets.Count > 0)
            {
                return "Channels";
            }
        }

        return Descriptor.ChannelName.Length > 0 ? "Users" : _server != null ? "Channels" : "Details";
    }

    private async void CommitDraft()
    {
        var draft = ComposerTextBox.Text.Trim();
        if (draft.Length == 0)
        {
            return;
        }

        if (draft.StartsWith('/') && TryHandleLocalCommand(draft))
        {
            ComposerTextBox.Text = string.Empty;
            return;
        }

        if (_server != null)
        {
            await _context.IrcRuntime.SendAsync(_server, Descriptor.ChannelName, draft);
        }
        else if (draft.StartsWith('/'))
        {
            AddStatusMessage($"Captured local command preview: {draft}");
        }
        else
        {
            AddMessage(UserMessage("You", draft, DateTimeOffset.Now));
        }

        ComposerTextBox.Text = string.Empty;
        ScrollToLatestMessage();
    }

    private bool TryHandleLocalCommand(string draft)
    {
        if (_server == null)
        {
            return false;
        }

        var action = _context.WorkspaceSessions.InterpretCommand(_server, Descriptor.ChannelName, draft);
        switch (action.Kind)
        {
            case WorkspaceCommandActionKind.ShowStatus:
            {
                AddStatusMessage(action.Message);
                return true;
            }

            case WorkspaceCommandActionKind.OpenChannel:
            {
                foreach (var target in action.Targets)
                {
                    _openChannel?.Invoke(_server.Name, target);
                    _ = _context.IrcRuntime.OpenChannelAsync(_server, target);
                }

                AddStatusMessage(action.Message);
                return true;
            }

            case WorkspaceCommandActionKind.OpenQuery:
            {
                foreach (var target in action.Targets)
                {
                    _openQuery?.Invoke(_server.Name, target);
                }

                AddStatusMessage(action.Message);
                return true;
            }

            case WorkspaceCommandActionKind.CloseSession:
            {
                _ = _context.IrcRuntime.SendAsync(_server, Descriptor.ChannelName, "/part");
                _closeSession?.Invoke(Descriptor);
                return true;
            }

            case WorkspaceCommandActionKind.DeferToTransport:
                return false;
        }

        return false;
    }

    private void AttachLiveSession(IrcRuntimeSessionState session)
    {
        DetachLiveSession();
        _liveSession = session;
        _liveSession.PropertyChanged += LiveSession_PropertyChanged;
        _liveSession.Messages.CollectionChanged += LiveMessages_CollectionChanged;
        _liveSession.Participants.CollectionChanged += LiveParticipants_CollectionChanged;
        _liveSession.JoinedTargets.CollectionChanged += LiveJoinedTargets_CollectionChanged;
        SyncUnreadFromLiveSession();
    }

    private void DetachLiveSession()
    {
        if (_liveSession == null)
        {
            return;
        }

        _liveSession.PropertyChanged -= LiveSession_PropertyChanged;
        _liveSession.Messages.CollectionChanged -= LiveMessages_CollectionChanged;
        _liveSession.Participants.CollectionChanged -= LiveParticipants_CollectionChanged;
        _liveSession.JoinedTargets.CollectionChanged -= LiveJoinedTargets_CollectionChanged;
        _liveSession = null;
        SyncUnreadFromLiveSession();
    }

    private void LiveSession_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RunOnUiThread(() =>
        {
            SyncUnreadFromLiveSession();
            RefreshLiveMetadata();
        });
    }

    private void LiveMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RunOnUiThread(() =>
        {
            if (_liveSession == null)
            {
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var runtimeMessage in e.NewItems.OfType<IrcRuntimeMessage>())
                {
                    AddMessage(MapMessage(runtimeMessage));
                }

                if (_isActiveView)
                {
                    _liveSession.MarkRead();
                }

                ScrollToLatestMessage();
                return;
            }

            SyncMessagesFromLiveSession();
        });
    }

    private void LiveParticipants_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RunOnUiThread(() =>
        {
            SyncRosterEntries();
            NotifyMetadataChanged();
        });
    }

    private void LiveJoinedTargets_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RunOnUiThread(() =>
        {
            SyncRosterEntries();
            SyncFacts();
            NotifyMetadataChanged();
        });
    }

    private void RefreshLiveSurface()
    {
        SyncMessagesFromLiveSession();
        RefreshLiveMetadata();
    }

    private void SyncMessagesFromLiveSession()
    {
        if (_liveSession == null)
        {
            return;
        }

        RebuildMessages(_liveSession.Messages.Select(MapMessage));
        ScrollToLatestMessage();
    }

    private void RefreshLiveMetadata()
    {
        SyncFacts();
        SyncRosterEntries();
        NotifyMetadataChanged();
    }

    private void SyncUnreadFromLiveSession()
    {
        if (_liveSession == null)
        {
            UnreadCount = 0;
            HasUnreadMention = false;
            return;
        }

        if (_isActiveView && (_liveSession.UnreadCount > 0 || _liveSession.HasUnreadMention))
        {
            _liveSession.MarkRead();
            return;
        }

        UnreadCount = _liveSession.UnreadCount;
        HasUnreadMention = _liveSession.HasUnreadMention;
    }

    private void AddStatusMessage(string text)
    {
        AddMessage(StatusMessage(text, DateTimeOffset.Now));
        ScrollToLatestMessage();
    }

    private void ScrollToLatestMessage()
    {
        if (Messages.Count == 0)
        {
            return;
        }

        MessagesList.ScrollIntoView(Messages.Last());
    }

    private ChannelMessageItem MapMessage(IrcRuntimeMessage message)
    {
        return CreateMessageItem(
            message.Author,
            message.Text,
            message.Timestamp,
            message.Kind,
            message.IsStatus,
            message.IsMention);
    }

    private ChannelMessageItem StatusMessage(string text, DateTimeOffset timestamp)
    {
        return CreateMessageItem(
            string.Empty,
            text,
            timestamp,
            IrcRuntimeMessageKind.Status,
            true,
            false);
    }

    private ChannelMessageItem UserMessage(string author, string text, DateTimeOffset timestamp)
    {
        return CreateMessageItem(
            author,
            text,
            timestamp,
            IrcRuntimeMessageKind.Normal,
            false,
            false);
    }

    private ChannelMessageItem CreateMessageItem(
        string author,
        string text,
        DateTimeOffset timestamp,
        IrcRuntimeMessageKind kind,
        bool isStatus,
        bool isMention)
    {
        var message = new ChannelMessageItem
        {
            Author = author,
            Kind = kind,
            Text = text,
            Timestamp = timestamp,
            IsStatus = isStatus,
            IsMention = isMention
        };

        if (!isStatus && !string.IsNullOrWhiteSpace(author))
        {
            var darkTheme = _context.SettingsStore.GetBoolean(ConfigKeys.DarkTheme, true);
            message.AuthorBrush = NickColorHelper.CreateNickBrush(author, darkTheme);
            message.MonogramBackgroundBrush = NickColorHelper.CreateNickChipBrush(author, darkTheme);
        }

        return message;
    }

    public void SetIsActiveView(bool isActive)
    {
        if (_isActiveView == isActive)
        {
            if (isActive)
            {
                ClearUnread();
            }

            return;
        }

        _isActiveView = isActive;
        if (isActive)
        {
            ClearUnread();
        }
    }

    private void ClearUnread()
    {
        if (_liveSession != null)
        {
            _liveSession.MarkRead();
            return;
        }

        UnreadCount = 0;
        HasUnreadMention = false;
    }

    public void RefreshFromSettings()
    {
        _showTopicBar = _context.SettingsStore.GetBoolean(ConfigKeys.ShowTopicBar, true);
        NotifyMetadataChanged();
        NotifyStyleChanged();
        ApplyMessageTemplate();
        if (_liveSession != null)
        {
            SyncMessagesFromLiveSession();
            return;
        }

        RebuildMessages(BuildInitialMessages());
    }

    private void NotifyMetadataChanged()
    {
        OnPropertyChanged(nameof(TopicText));
        OnPropertyChanged(nameof(HeaderIdentityVisibility));
        OnPropertyChanged(nameof(HeaderVisibility));
        OnPropertyChanged(nameof(SubtitleVisibility));
        OnPropertyChanged(nameof(DescriptionVisibility));
        OnPropertyChanged(nameof(FactHeaderText));
        OnPropertyChanged(nameof(RosterHeaderText));
        OnPropertyChanged(nameof(SidebarStatusText));
        OnPropertyChanged(nameof(SidebarPrimaryHeader));
        OnPropertyChanged(nameof(SidebarSecondaryHeader));
        OnPropertyChanged(nameof(TopicVisibility));
        OnPropertyChanged(nameof(TopicBarPadding));
        OnPropertyChanged(nameof(TopicBarFontSize));
    }

    private void NotifyStyleChanged()
    {
        OnPropertyChanged(nameof(ChatFontFamily));
        OnPropertyChanged(nameof(ChatFontSize));
        OnPropertyChanged(nameof(ChatLineHeight));
        OnPropertyChanged(nameof(PagePadding));
        OnPropertyChanged(nameof(SectionSpacing));
        OnPropertyChanged(nameof(HeaderPadding));
        OnPropertyChanged(nameof(TopicBarPadding));
        OnPropertyChanged(nameof(TopicBarFontSize));
        OnPropertyChanged(nameof(ConversationRowSpacing));
        OnPropertyChanged(nameof(ConversationHeaderPadding));
        OnPropertyChanged(nameof(MessagesPadding));
        OnPropertyChanged(nameof(ComposerPadding));
        OnPropertyChanged(nameof(MessageTemplate));
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void AddMessage(ChannelMessageItem message)
    {
        var includeJoinLeave = !_context.SettingsStore.GetBoolean(ConfigKeys.IgnoreJoinLeave, false);
        if (!includeJoinLeave && message.Kind == IrcRuntimeMessageKind.JoinPart)
        {
            return;
        }

        var modernChat = _context.SettingsStore.GetBoolean(ConfigKeys.ModernChat, true);
        if (modernChat && Messages.LastOrDefault() is { } currentGroup && currentGroup.CanMerge(message))
        {
            currentGroup.Lines.Add(message);
            return;
        }

        var group = new ChannelMessageGroupItem
        {
            Parent = message
        };
        group.Lines.Add(message);
        Messages.Add(group);
    }

    private void RebuildMessages(IEnumerable<ChannelMessageItem> source)
    {
        var includeJoinLeave = !_context.SettingsStore.GetBoolean(ConfigKeys.IgnoreJoinLeave, false);
        var modernChat = _context.SettingsStore.GetBoolean(ConfigKeys.ModernChat, true);
        var nextGroups = new List<ChannelMessageGroupItem>();

        foreach (var message in source)
        {
            if (!includeJoinLeave && message.Kind == IrcRuntimeMessageKind.JoinPart)
            {
                continue;
            }

            if (!modernChat)
            {
                var singleGroup = new ChannelMessageGroupItem
                {
                    Parent = message
                };
                singleGroup.Lines.Add(message);
                nextGroups.Add(singleGroup);
                continue;
            }

            var currentGroup = nextGroups.LastOrDefault();
            if (currentGroup != null && currentGroup.CanMerge(message))
            {
                currentGroup.Lines.Add(message);
                continue;
            }

            var group = new ChannelMessageGroupItem
            {
                Parent = message
            };
            group.Lines.Add(message);
            nextGroups.Add(group);
        }

        Messages.Clear();
        foreach (var group in nextGroups)
        {
            Messages.Add(group);
        }

        ApplyMessageTemplate();
    }

    private void SyncFacts()
    {
        ReplaceCollection(Facts, BuildFacts());
    }

    private void SyncRosterEntries()
    {
        ReplaceCollection(RosterEntries, BuildRosterEntries());
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        var nextItems = source.ToArray();
        if (target.Count == nextItems.Length && target.SequenceEqual(nextItems))
        {
            return;
        }

        target.Clear();
        foreach (var item in nextItems)
        {
            target.Add(item);
        }
    }

    private void ApplyMessageTemplate()
    {
        if (MessagesList == null)
        {
            return;
        }

        MessagesList.ItemTemplate = MessageTemplate;
    }

    private static bool IsChannelTarget(string target)
    {
        return target.StartsWith("#", StringComparison.Ordinal) ||
               target.StartsWith("&", StringComparison.Ordinal) ||
               target.StartsWith("+", StringComparison.Ordinal) ||
               target.StartsWith("!", StringComparison.Ordinal);
    }

    private static string NormalizeSidebarTarget(string item)
    {
        return item.TrimStart('~', '&', '@', '%', '+').Trim();
    }

    private void RunOnUiThread(Action action)
    {
        if (DispatcherQueue == null || DispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }

        _ = DispatcherQueue.TryEnqueue(() => action());
    }
}
