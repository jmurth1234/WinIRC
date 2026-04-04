using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinIRC.Core;
using WinIRC.Core.Models;
using WinIRC.WinUI.Interfaces;
using WinIRC.WinUI.Models;
using WinIRC.WinUI.Services;
using WinIRC.WinUI.Views;

namespace WinIRC.WinUI;

public sealed partial class ShellPage : Page, INotifyPropertyChanged
{
    private readonly AppBootstrapContext _context;
    private bool _initialized;
    private bool _workspaceTargetsSubscribed;
    private SavedServer? _activeServer;
    private SessionTab? _selectedTab;
    private ISessionSidebarSource? _selectedSidebarSource;
    private bool _isTopicBarEnabled = true;
    private bool _isSidebarEnabled = true;
    private Border? _activePaneSplitter;
    private double _dragOriginX;
    private double _dragOriginWidth;
    private readonly Dictionary<string, (IrcRuntimeSessionState State, PropertyChangedEventHandler Handler)> _sidebarRuntimeSubscriptions = new(StringComparer.OrdinalIgnoreCase);
    private bool _syncingSidebarSelection;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UIElement? TitleBarElement => TitleBarDragRegion;

    public ObservableCollection<SavedServer> Servers { get; } = new();

    public ObservableCollection<WorkspaceTarget> WorkspaceTargets { get; } = new();

    public ObservableCollection<SidebarTreeItem> SidebarTreeItems { get; } = new();

    public ObservableCollection<SessionTab> Tabs { get; } = new();

    public SessionTab? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab == value)
            {
                return;
            }

            _selectedTab = value;
            UpdateActiveWorkspaceFromSelection();
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanDetachSelectedTab));
            UpdateSelectedSidebarSource();
            SyncSessionActivityState();
            SyncSidebarSelection();
        }
    }

    public string SelectedSidebarTitle => _selectedSidebarSource?.SidebarTitle ?? "Session";

    public string SelectedSidebarSubtitle => _selectedSidebarSource?.SidebarSubtitle ?? string.Empty;

    public string SelectedSidebarStatus => _selectedSidebarSource?.SidebarStatusText ?? string.Empty;

    public Visibility SelectedSidebarSubtitleVisibility => string.IsNullOrWhiteSpace(SelectedSidebarSubtitle)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public Visibility SelectedSidebarStatusVisibility => string.IsNullOrWhiteSpace(SelectedSidebarStatus)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public string SelectedSidebarPrimaryHeader => _selectedSidebarSource?.SidebarPrimaryHeader ?? "Status";

    public ObservableCollection<ChannelFactItem>? SelectedSidebarPrimaryItems => _selectedSidebarSource?.SidebarPrimaryItems;

    public string SelectedSidebarSecondaryHeader => _selectedSidebarSource?.SidebarSecondaryHeader ?? "Targets";

    public ObservableCollection<string>? SelectedSidebarSecondaryItems => _selectedSidebarSource?.SidebarSecondaryItems;

    public bool CanActivateSelectedSidebarSecondaryItem => _selectedSidebarSource?.CanActivateSidebarSecondaryItem == true;

    public Visibility SelectedSidebarPrimaryVisibility => SelectedSidebarPrimaryItems?.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility SelectedSidebarSecondaryVisibility => SelectedSidebarSecondaryItems?.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public bool CanDetachSelectedTab => SelectedTab?.CanDetach == true;

    public bool CanToggleTopicBar => SelectedTab?.Content is ChannelPage channelPage && channelPage.SupportsTopicBar;

    public bool IsTopicBarEnabled
    {
        get => _isTopicBarEnabled;
        private set
        {
            if (_isTopicBarEnabled == value)
            {
                return;
            }

            _isTopicBarEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsSidebarEnabled
    {
        get => _isSidebarEnabled;
        private set
        {
            if (_isSidebarEnabled == value)
            {
                return;
            }

            _isSidebarEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SidebarVisibility));
        }
    }

    public Visibility SidebarVisibility => IsSidebarEnabled ? Visibility.Visible : Visibility.Collapsed;

    public string AppTagline => "Internet Relay Chat";

    public Visibility AppTaglineVisibility => string.IsNullOrWhiteSpace(AppTagline)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public string FooterText => string.Empty;

    public Visibility FooterVisibility => string.IsNullOrWhiteSpace(FooterText)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public string WorkspaceTitle => _activeServer?.Name ?? string.Empty;

    public string WorkspaceSubtitle => _activeServer == null
        ? string.Empty
        : "Channels";

    public Visibility WorkspaceSectionVisibility => _activeServer == null
        ? Visibility.Collapsed
        : Visibility.Visible;

    public string MigrationSummary
    {
        get
        {
            var importedData = _context.SettingsStore.GetBoolean(ConfigKeys.MigrationImportedData);
            var importedSettings = _context.SettingsStore.GetInt(ConfigKeys.MigrationImportedSettingsCount);
            var importedServers = _context.SettingsStore.GetInt(ConfigKeys.MigrationImportedServersCount);
            return importedData
                ? $"Imported {importedSettings} settings values and {importedServers} saved servers from the packaged UWP state."
                : "No prior packaged state was detected for migration, so the WinUI app is currently using a clean local state store.";
        }
    }

    public ShellPage(AppBootstrapContext context)
    {
        _context = context;
        InitializeComponent();

        Loaded += ShellPage_Loaded;
        Unloaded += ShellPage_Unloaded;
    }

    public async Task ApplyPendingProtocolAsync()
    {
        var pendingUri = _context.ProtocolActivationService.ConsumePendingProtocolUri();
        if (pendingUri == null)
        {
            return;
        }

        OpenProtocolTab(pendingUri);
        await _context.NotificationService.ShowInfoAsync("Protocol activation ready", pendingUri.ToString());
    }

    public void ApplyTheme(ElementTheme theme)
    {
        RequestedTheme = theme;
        var sessionTabs = ResolveSessionTabs();
        if (sessionTabs == null)
        {
            LogThemeState($"ApplyTheme({theme}) skipped: SessionTabs unavailable");
            return;
        }

        sessionTabs.RequestedTheme = theme;
        ApplyShellChrome(theme);

        foreach (var tabContent in sessionTabs.TabItems
                     .OfType<TabViewItem>()
                     .Select(item => item.Content)
                     .OfType<FrameworkElement>())
        {
            tabContent.RequestedTheme = theme;

            if (tabContent is SettingsPage settingsPage)
            {
                settingsPage.RefreshFromSettings();
            }
        }

        LogThemeState($"ApplyTheme({theme})");
    }

    private async void ShellPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_workspaceTargetsSubscribed)
        {
            _context.WorkspaceSessions.TargetsChanged += WorkspaceSessions_TargetsChanged;
            _workspaceTargetsSubscribed = true;
        }

        LogThemeState("Loaded");

        if (_initialized)
        {
            await ApplyPendingProtocolAsync();
            return;
        }

        _initialized = true;
        await InitializeAsync();
    }

    private void ShellPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_workspaceTargetsSubscribed)
        {
            _context.WorkspaceSessions.TargetsChanged -= WorkspaceSessions_TargetsChanged;
            _workspaceTargetsSubscribed = false;
        }

        DetachSelectedSidebarSource();
    }

    private async Task InitializeAsync()
    {
        LoadViewSettings();
        await ApplyThemeFromSettingsAsync();

        await RefreshServersAsync();

        OpenWelcomeTab();

        foreach (var server in Servers.Where(server => server.AutoConnect))
        {
            OpenServerTab(server);
        }

        await ApplyPendingProtocolAsync();

        OnPropertyChanged(nameof(MigrationSummary));
        OnPropertyChanged(nameof(FooterText));
        UpdateSelectedSidebarSource();
        ApplyViewSettings();
    }

    private Task ApplyThemeFromSettingsAsync()
    {
        var theme = _context.SettingsStore.GetBoolean(ConfigKeys.DarkTheme, true)
            ? ElementTheme.Dark
            : ElementTheme.Light;

        if (App.MainWindow is MainWindow mainWindow)
        {
            mainWindow.ApplyTheme();
        }
        else
        {
            ApplyTheme(theme);
        }

        return Task.CompletedTask;
    }

    private void OpenWelcomeTab()
    {
        // Keep shell copy user-facing; port-progress notes belong in code, not in the app surface.
        var descriptor = new ChatSessionDescriptor
        {
            Kind = ChatSessionKind.Welcome,
            Header = "Welcome",
            Subtitle = string.Empty,
            Description = "Choose a network from the left, or use WinIRC > Manage Servers."
        };

        EnsureTab(descriptor, () => new ChannelPage(descriptor, null, _context));
    }

    private void OpenProtocolTab(Uri uri)
    {
        var descriptor = new ChatSessionDescriptor
        {
            Kind = ChatSessionKind.ProtocolActivation,
            Header = uri.Host.Length > 0 ? uri.Host : uri.Scheme,
            Subtitle = uri.ToString(),
            Description = "Opened from an external link.",
            SourceUri = uri.ToString()
        };

        EnsureTab(descriptor, () => CreateChannelPage(descriptor, null));
    }

    private void OpenServerTab(SavedServer server)
    {
        EnsureWorkspaceSnapshot(server);
        var descriptor = new ChatSessionDescriptor
        {
            Kind = ChatSessionKind.Server,
            Header = server.Name,
            Subtitle = server.DisplayEndpoint,
            ServerName = server.Name,
            Description = string.Empty
        };

        EnsureTab(descriptor, () => CreateChannelPage(descriptor, server));
    }

    private void OpenChannelTab(string serverName, string channelName)
    {
        var server = FindServer(serverName);
        if (server == null)
        {
            return;
        }

        var normalizedChannel = channelName.Trim();
        if (normalizedChannel.Length == 0)
        {
            return;
        }

        if (NamesMatch(normalizedChannel, "Server"))
        {
            OpenServerTab(server);
            return;
        }

        EnsureWorkspaceTarget(server.Name, normalizedChannel, isDirectMessage: !normalizedChannel.StartsWith("#", StringComparison.Ordinal));

        var isDirectMessage = !normalizedChannel.StartsWith("#", StringComparison.Ordinal);
        var descriptor = new ChatSessionDescriptor
        {
            Kind = ChatSessionKind.Channel,
            Header = isDirectMessage ? "@" + normalizedChannel : normalizedChannel,
            Subtitle = server.DisplayEndpoint,
            ServerName = server.Name,
            ChannelName = normalizedChannel,
            Description = string.Empty
        };

        EnsureTab(descriptor, () => CreateChannelPage(descriptor, server));
    }

    private async Task RefreshServersAsync()
    {
        Servers.Clear();
        foreach (var server in await _context.ServerRepository.LoadAsync())
        {
            Servers.Add(server);
        }

        _context.WorkspaceSessions.Reset(Servers);
        RefreshConnectSubMenu();
        UpdateActiveWorkspaceFromSelection();
        RebuildSidebarTree();
        OnPropertyChanged(nameof(FooterText));
    }

    private async Task SaveServerAsync(string? originalName, SavedServer server)
    {
        var existing = originalName == null
            ? Servers.FirstOrDefault(savedServer => NamesMatch(savedServer.Name, server.Name))
            : Servers.FirstOrDefault(savedServer => NamesMatch(savedServer.Name, originalName));

        if (existing != null)
        {
            var index = Servers.IndexOf(existing);
            Servers[index] = server;
        }
        else
        {
            Servers.Add(server);
        }

        EnsureWorkspaceSnapshot(server);
        if (_activeServer != null && NamesMatch(_activeServer.Name, server.Name))
        {
            _activeServer = server;
            RefreshWorkspaceTargets();
        }

        await _context.ServerRepository.SaveAsync(Servers);
        _context.WorkspaceSessions.Reset(Servers);
        RefreshConnectSubMenu();
        RebuildSidebarTree();
        OnPropertyChanged(nameof(FooterText));
    }

    private async Task DeleteServerAsync(string serverName)
    {
        var existing = Servers.FirstOrDefault(server => NamesMatch(server.Name, serverName));
        if (existing == null)
        {
            return;
        }

        Servers.Remove(existing);
        if (_activeServer != null && NamesMatch(_activeServer.Name, existing.Name))
        {
            _activeServer = null;
            RefreshWorkspaceTargets();
        }
        await _context.ServerRepository.SaveAsync(Servers);
        _context.WorkspaceSessions.Reset(Servers);
        RefreshConnectSubMenu();
        RebuildSidebarTree();
        OnPropertyChanged(nameof(FooterText));
    }

    private void EnsureTab(ChatSessionDescriptor descriptor, Func<UIElement> contentFactory)
    {
        var sessionTabs = ResolveSessionTabs();
        if (sessionTabs == null)
        {
            LogThemeState($"EnsureTab({descriptor.Header}) skipped: SessionTabs unavailable");
            return;
        }

        var identityKey = descriptor.Kind + "|" + descriptor.ServerName + "|" + descriptor.ChannelName + "|" + descriptor.SourceUri;
        var existing = Tabs.FirstOrDefault(tab => tab.IdentityKey == identityKey);
        if (existing != null)
        {
            var existingView = sessionTabs.TabItems
                .OfType<TabViewItem>()
                .FirstOrDefault(item => ReferenceEquals(item.Tag, existing));
            if (existingView != null)
            {
                sessionTabs.SelectedItem = existingView;
            }

            SelectedTab = existing;
            return;
        }

        var tab = new SessionTab
        {
            Descriptor = descriptor,
            Content = contentFactory()
        };

        if (tab.Content is FrameworkElement themedContent)
        {
            themedContent.RequestedTheme = RequestedTheme;
        }

        BindTabContent(tab);

        Tabs.Add(tab);
        var tabViewItem = new TabViewItem
        {
            Header = tab.Header,
            Content = tab.Content,
            Tag = tab,
            IsClosable = true
        };
        sessionTabs.TabItems.Add(tabViewItem);
        sessionTabs.SelectedItem = tabViewItem;
        SelectedTab = tab;
    }

    private void SidebarTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        var item = args.InvokedItem as SidebarTreeItem;
        if (item == null && args.InvokedItem is TreeViewNode { Content: SidebarTreeItem nodeItem })
        {
            item = nodeItem;
        }

        if (item == null)
        {
            return;
        }

        if (item.IsServer)
        {
            OpenServerTab(item.Server);
            return;
        }

        if (item.Target == null)
        {
            return;
        }

        OpenWorkspaceTarget(item.Server, item.Target);
    }

    private void SidebarTreeView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement targetElement
            || !TryGetDataContext(e.OriginalSource as DependencyObject, out SidebarTreeItem? item)
            || item == null)
        {
            return;
        }

        if (item.IsServer)
        {
            ShowServerMenu(targetElement, e.GetPosition(targetElement), item.Server);
        }
        else if (item.Target != null)
        {
            ShowWorkspaceTargetMenu(targetElement, e.GetPosition(targetElement), item.Server, item.Target);
        }

        e.Handled = true;
    }

    private void SidebarRosterList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not string item)
        {
            return;
        }

        if (SelectedSidebarSecondaryHeader == "Users")
        {
            switch (GetUserListClickMode())
            {
                case 0:
                    _selectedSidebarSource?.InsertSidebarSecondaryItem(item);
                    return;
                case 2:
                {
                    if (sender is FrameworkElement targetElement
                        && SelectedTab?.Descriptor.ServerName is { Length: > 0 } serverName
                        && FindServer(serverName) is SavedServer server)
                    {
                        ShowSidebarRosterMenu(targetElement, null, item, server, SelectedTab?.Descriptor.ChannelName ?? string.Empty, true);
                    }

                    return;
                }
            }
        }

        _selectedSidebarSource?.ActivateSidebarSecondaryItem(item);
    }

    private void SidebarRosterList_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement targetElement)
        {
            return;
        }

        if (!TryGetSidebarRosterContext(e.OriginalSource as DependencyObject, out var selectedItem, out var server, out var channelName, out var isUser))
        {
            return;
        }

        ShowSidebarRosterMenu(targetElement, e.GetPosition(targetElement), selectedItem, server, channelName, isUser);
        e.Handled = true;
    }

    private async Task ShowSettingsDialogAsync(SettingsSection initialSection = SettingsSection.General, string title = "Settings")
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            Content = new SettingsPage(_context, ApplyShellSettingsAsync, initialSection)
        };

        await dialog.ShowAsync();
    }

    private async Task ShowConnectDialogAsync()
    {
        await ShowConnectDialogAsync(null);
    }

    private async Task ShowConnectDialogAsync(string? initialServerName)
    {
        ContentDialog? dialog = null;
        dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Manage Networks",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close
        };

        dialog.Content = new ConnectPage(
            _context,
            Servers,
            SaveServerAsync,
            DeleteServerAsync,
            OpenServerTab,
            () => dialog.Hide(),
            initialServerName);

        await dialog.ShowAsync();
    }

    private async void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        await ShowSettingsDialogAsync();
    }

    private async void ManageNetworks_Click(object sender, RoutedEventArgs e)
    {
        await ShowConnectDialogAsync();
    }

    private async void ManageNetworksMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await ShowConnectDialogAsync();
    }

    private void NewWelcomeTab_Click(object sender, RoutedEventArgs e)
    {
        OpenWelcomeTab();
    }

    private async void AppearanceSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await ShowSettingsDialogAsync(SettingsSection.Appearance, "Appearance");
    }

    private async void ConnectionSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await ShowSettingsDialogAsync(SettingsSection.Connection, "Connection");
    }

    private async void BehaviourSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await ShowSettingsDialogAsync(SettingsSection.Behaviour, "Behaviour");
    }

    private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await ShowSettingsDialogAsync(SettingsSection.About, "About WinIRC");
    }

    private void PaneSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border splitter || RootGrid == null)
        {
            return;
        }

        _activePaneSplitter = splitter;
        _dragOriginX = e.GetCurrentPoint(RootGrid).Position.X;
        _dragOriginWidth = ReferenceEquals(splitter, LeftPaneSplitter)
            ? LeftRailColumn.ActualWidth
            : RightRailColumn.ActualWidth;

        splitter.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void PaneSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_activePaneSplitter == null || RootGrid == null)
        {
            return;
        }

        var currentX = e.GetCurrentPoint(RootGrid).Position.X;
        var delta = currentX - _dragOriginX;
        var nextWidth = ReferenceEquals(_activePaneSplitter, LeftPaneSplitter)
            ? _dragOriginWidth + delta
            : _dragOriginWidth - delta;

        nextWidth = Math.Clamp(nextWidth, 160, 360);

        if (ReferenceEquals(_activePaneSplitter, LeftPaneSplitter))
        {
            LeftRailColumn.Width = new GridLength(nextWidth);
        }
        else
        {
            RightRailColumn.Width = new GridLength(nextWidth);
        }

        e.Handled = true;
    }

    private void PaneSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ReleasePaneSplitter();
        e.Handled = true;
    }

    private void PaneSplitter_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        ReleasePaneSplitter();
    }

    private void SessionTabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is not TabViewItem tabViewItem || tabViewItem.Tag is not SessionTab tab)
        {
            return;
        }

        CloseTab(tab);
    }

    private void SessionTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResolveSessionTabs()?.SelectedItem is TabViewItem tabViewItem && tabViewItem.Tag is SessionTab tab)
        {
            SelectedTab = tab;
        }
    }

    private async void TopicBarMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (TopicBarMenuItem == null)
        {
            return;
        }

        await _context.SettingsStore.SetBooleanAsync(ConfigKeys.ShowTopicBar, TopicBarMenuItem.IsChecked);
        LoadViewSettings();
        ApplyViewSettings();
    }

    private async void SidebarMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (SidebarMenuItem == null)
        {
            return;
        }

        await _context.SettingsStore.SetBooleanAsync(ConfigKeys.ShowSidebar, SidebarMenuItem.IsChecked);
        LoadViewSettings();
        ApplyViewSettings();
    }

    private void RefreshConnectSubMenu()
    {
        if (ConnectSubMenu == null)
        {
            return;
        }

        ConnectSubMenu.Items.Clear();

        if (Servers.Count == 0)
        {
            ConnectSubMenu.Items.Add(new MenuFlyoutItem
            {
                Text = "No saved networks",
                IsEnabled = false
            });

            return;
        }

        foreach (var server in Servers.OrderBy(server => server.Name, StringComparer.OrdinalIgnoreCase))
        {
            var item = new MenuFlyoutItem
            {
                Text = server.Name,
                Tag = server.Name
            };

            item.Click += ConnectSubMenuItem_Click;
            ConnectSubMenu.Items.Add(item);
        }
    }

    private void ConnectSubMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string serverName })
        {
            return;
        }

        var server = FindServer(serverName);
        if (server != null)
        {
            OpenServerTab(server);
        }
    }

    private async void Detach_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedTab == null || !SelectedTab.CanDetach)
        {
            return;
        }

        await _context.WindowService.ShowDetachedSessionAsync(SelectedTab.Descriptor);
    }

    private void ShowServerMenu(FrameworkElement targetElement, Point offset, SavedServer server)
    {
        var menu = new MenuFlyout();
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = server.Name,
            IsEnabled = false
        });
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(CreateSidebarMenuItem("Open Server", () => OpenServerTab(server)));
        menu.Items.Add(CreateSidebarMenuItem("Join Channel...", async () => await ShowJoinChannelDialogAsync(server)));
        menu.Items.Add(CreateSidebarMenuItem("Manage Network...", async () => await ShowConnectDialogAsync(server.Name)));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(CreateSidebarMenuItem("Delete Network", async () => await DeleteServerWithConfirmationAsync(server.Name)));
        menu.ShowAt(targetElement, offset);
    }

    private void ShowWorkspaceTargetMenu(FrameworkElement targetElement, Point offset, SavedServer server, WorkspaceTarget target)
    {
        var menu = new MenuFlyout();
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = target.DisplayName,
            IsEnabled = false
        });
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(CreateSidebarMenuItem("Open Target", () => OpenWorkspaceTarget(server, target)));

        if (!NamesMatch(target.Name, "Server"))
        {
            if (FindChannelTab(server.Name, target.Name) != null)
            {
                menu.Items.Add(CreateSidebarMenuItem("Close Tab", () => CloseChannelTab(server.Name, target.Name)));
            }

            if (!target.IsDirectMessage)
            {
                var alwaysNotify = new ToggleMenuFlyoutItem
                {
                    Text = "Notify for all messages",
                    IsChecked = _context.SettingsStore.GetBoolean(
                        ConfigKeys.PerChannelSetting(server.Name, target.Name, ConfigKeys.AlwaysNotify),
                        false)
                };
                alwaysNotify.Click += async (_, _) =>
                {
                    await _context.SettingsStore.SetBooleanAsync(
                        ConfigKeys.PerChannelSetting(server.Name, target.Name, ConfigKeys.AlwaysNotify),
                        alwaysNotify.IsChecked);
                };
                menu.Items.Add(alwaysNotify);

                if (FindChannelTab(server.Name, target.Name) != null)
                {
                    menu.Items.Add(CreateSidebarMenuItem("Part Channel", async () => await PartAndCloseTargetAsync(server, target.Name)));
                }
            }
        }

        menu.ShowAt(targetElement, offset);
    }

    private async void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        await ToggleThemeAsync();
    }

    private async void QuitClient_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Are you sure?",
            Content = "This will close WinIRC and end all current connections.",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            Application.Current.Exit();
        }
    }

    private async Task ShowJoinChannelDialogAsync(SavedServer server)
    {
        var textBox = new TextBox
        {
            PlaceholderText = "#channel",
            Text = "#"
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Join channel on {server.Name}",
            Content = textBox,
            PrimaryButtonText = "Join",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        var channelName = textBox.Text.Trim();
        if (channelName.Length == 0)
        {
            return;
        }

        if (!channelName.StartsWith("#", StringComparison.Ordinal)
            && !channelName.StartsWith("&", StringComparison.Ordinal)
            && !channelName.StartsWith("+", StringComparison.Ordinal)
            && !channelName.StartsWith("!", StringComparison.Ordinal))
        {
            channelName = "#" + channelName;
        }

        OpenChannelTab(server.Name, channelName);
        await _context.IrcRuntime.OpenChannelAsync(server, channelName);
    }

    private async Task DeleteServerWithConfirmationAsync(string serverName)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Delete saved network?",
            Content = "This removes the saved network from local state.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        await DeleteServerAsync(serverName);
    }

    private async Task ToggleThemeAsync()
    {
        var darkTheme = _context.SettingsStore.GetBoolean(ConfigKeys.DarkTheme, true);
        await _context.SettingsStore.SetBooleanAsync(ConfigKeys.DarkTheme, !darkTheme);
        await ApplyShellSettingsAsync();
    }

    private async Task ApplyShellSettingsAsync()
    {
        LoadViewSettings();
        await ApplyThemeFromSettingsAsync();

        foreach (var tabContent in Tabs
                     .Select(tab => tab.Content)
                     .OfType<ChannelPage>())
        {
            tabContent.RefreshFromSettings();
        }

        RefreshSelectedSidebar();
        ApplyViewSettings();
    }

    private ChannelPage CreateChannelPage(ChatSessionDescriptor descriptor, SavedServer? server)
    {
        return new ChannelPage(
            descriptor,
            server,
            _context,
            OpenChannelTab,
            OpenChannelTab,
            CloseSession);
    }

    private void CloseSession(ChatSessionDescriptor descriptor)
    {
        var tab = Tabs.FirstOrDefault(candidate => candidate.IdentityKey == descriptor.Kind + "|" + descriptor.ServerName + "|" + descriptor.ChannelName + "|" + descriptor.SourceUri);
        if (tab != null)
        {
            CloseTab(tab);
        }
    }

    private void OpenWorkspaceTarget(SavedServer server, WorkspaceTarget target)
    {
        if (NamesMatch(target.Name, "Server"))
        {
            OpenServerTab(server);
            return;
        }

        OpenChannelTab(server.Name, target.Name);
    }

    private SessionTab? FindChannelTab(string serverName, string channelName)
    {
        return Tabs.FirstOrDefault(tab =>
            tab.Descriptor.Kind == ChatSessionKind.Channel &&
            NamesMatch(tab.Descriptor.ServerName, serverName) &&
            NamesMatch(tab.Descriptor.ChannelName, channelName));
    }

    private void CloseChannelTab(string serverName, string channelName)
    {
        var tab = FindChannelTab(serverName, channelName);
        if (tab != null)
        {
            CloseTab(tab);
        }
    }

    private async Task PartAndCloseTargetAsync(SavedServer server, string channelName)
    {
        await _context.IrcRuntime.SendAsync(server, channelName, "/part");
        CloseChannelTab(server.Name, channelName);
    }

    private void CloseTab(SessionTab tab)
    {
        var sessionTabs = ResolveSessionTabs();
        if (sessionTabs == null)
        {
            return;
        }

        var tabViewItem = sessionTabs.TabItems
            .OfType<TabViewItem>()
            .FirstOrDefault(item => ReferenceEquals(item.Tag, tab));

        if (tabViewItem == null)
        {
            return;
        }

        UnbindTabContent(tab);
        sessionTabs.TabItems.Remove(tabViewItem);
        Tabs.Remove(tab);

        PruneWorkspaceTarget(tab.Descriptor);

        if (Tabs.Count == 0)
        {
            OpenWelcomeTab();
            return;
        }

        var nextSelection = sessionTabs.TabItems.OfType<TabViewItem>().LastOrDefault();
        if (nextSelection != null)
        {
            sessionTabs.SelectedItem = nextSelection;
            SelectedTab = nextSelection.Tag as SessionTab;
        }
    }

    private void EnsureWorkspaceSnapshot(SavedServer server)
    {
        _context.WorkspaceSessions.Reset(Servers);
    }

    private void EnsureWorkspaceTarget(string serverName, string targetName, bool isDirectMessage)
    {
        _context.WorkspaceSessions.AddTransientTarget(serverName, targetName, isDirectMessage);
        RefreshWorkspaceTargets();
    }

    private void PruneWorkspaceTarget(ChatSessionDescriptor descriptor)
    {
        if (descriptor.Kind != ChatSessionKind.Channel || descriptor.ServerName.Length == 0 || descriptor.ChannelName.Length == 0)
        {
            return;
        }

        var stillOpen = Tabs.Any(tab =>
            tab.Descriptor.Kind == ChatSessionKind.Channel &&
            NamesMatch(tab.Descriptor.ServerName, descriptor.ServerName) &&
            NamesMatch(tab.Descriptor.ChannelName, descriptor.ChannelName));

        if (stillOpen)
        {
            return;
        }

        var server = FindServer(descriptor.ServerName);
        var stillJoined = server != null
            && _context.IrcRuntime.TryGetTargetInfo(server, descriptor.ChannelName) != null;

        if (!stillJoined)
        {
            _context.WorkspaceSessions.RemoveTransientTarget(descriptor.ServerName, descriptor.ChannelName);
            RefreshWorkspaceTargets();
        }
    }

    private void UpdateActiveWorkspaceFromSelection()
    {
        var serverName = _selectedTab?.Descriptor.ServerName;
        _activeServer = string.IsNullOrWhiteSpace(serverName) ? null : FindServer(serverName);

        if (_activeServer != null)
        {
            EnsureWorkspaceSnapshot(_activeServer);
        }

        RefreshWorkspaceTargets();
    }

    private void RefreshWorkspaceTargets()
    {
        WorkspaceTargets.Clear();

        if (_activeServer != null)
        {
            foreach (var target in _context.WorkspaceSessions.GetTargets(_activeServer.Name))
            {
                WorkspaceTargets.Add(target);
            }
        }

        RebuildSidebarTree();
        OnPropertyChanged(nameof(WorkspaceTitle));
        OnPropertyChanged(nameof(WorkspaceSubtitle));
        OnPropertyChanged(nameof(WorkspaceSectionVisibility));
    }

    private void RebuildSidebarTree()
    {
        var desiredServerNames = new HashSet<string>(Servers.Select(server => server.Name), StringComparer.OrdinalIgnoreCase);
        for (var index = SidebarTreeItems.Count - 1; index >= 0; index--)
        {
            if (!desiredServerNames.Contains(SidebarTreeItems[index].Server.Name))
            {
                SidebarTreeItems.RemoveAt(index);
            }
        }

        for (var index = 0; index < Servers.Count; index++)
        {
            var server = Servers[index];
            var existing = SidebarTreeItems.FirstOrDefault(item => item.IsServer && NamesMatch(item.Server.Name, server.Name));
            if (existing == null)
            {
                existing = new SidebarTreeItem
                {
                    Server = server,
                    SecondaryText = BuildServerTreeSecondaryText(server)
                };
                ApplySidebarNotificationState(existing);
                SidebarTreeItems.Insert(index, existing);
            }
            else
            {
                existing.Server = server;
                existing.SecondaryText = BuildServerTreeSecondaryText(server);
                ApplySidebarNotificationState(existing);
                var currentIndex = SidebarTreeItems.IndexOf(existing);
                if (currentIndex != index)
                {
                    SidebarTreeItems.Move(currentIndex, index);
                }
            }

            SyncSidebarChildren(existing, server);
            existing.IsExpanded = ShouldExpandServerItem(existing, server);
        }

        SyncSidebarRuntimeSubscriptions();
        SyncSidebarSelection();
    }

    private void SyncSidebarChildren(SidebarTreeItem serverItem, SavedServer server)
    {
        var targets = _context.WorkspaceSessions
            .GetTargets(server.Name)
            .Where(target => !NamesMatch(target.Name, "Server"))
            .ToArray();
        var desiredTargetNames = new HashSet<string>(targets.Select(target => target.Name), StringComparer.OrdinalIgnoreCase);

        for (var index = serverItem.Children.Count - 1; index >= 0; index--)
        {
            var child = serverItem.Children[index];
            if (child.Target == null || !desiredTargetNames.Contains(child.Target.Name))
            {
                serverItem.Children.RemoveAt(index);
            }
        }

        for (var index = 0; index < targets.Length; index++)
        {
            var target = targets[index];
            var existing = serverItem.Children.FirstOrDefault(item => item.Target != null && NamesMatch(item.Target.Name, target.Name));
            if (existing == null)
            {
                existing = new SidebarTreeItem
                {
                    Server = server,
                    Target = target,
                    SecondaryText = BuildTargetTreeSecondaryText(server, target)
                };
                ApplySidebarNotificationState(existing);
                serverItem.Children.Insert(index, existing);
            }
            else
            {
                existing.Server = server;
                existing.Target = target;
                existing.SecondaryText = BuildTargetTreeSecondaryText(server, target);
                ApplySidebarNotificationState(existing);
                var currentIndex = serverItem.Children.IndexOf(existing);
                if (currentIndex != index)
                {
                    serverItem.Children.Move(currentIndex, index);
                }
            }
        }

        serverItem.IsExpanded = ShouldExpandServerItem(serverItem, server);
    }

    private bool ShouldExpandServerItem(SidebarTreeItem serverItem, SavedServer server)
    {
        if (serverItem.Children.Count == 0)
        {
            return false;
        }

        if (serverItem.IsExpanded)
        {
            return true;
        }

        if (_activeServer != null && NamesMatch(_activeServer.Name, server.Name))
        {
            return true;
        }

        return Tabs.Any(tab =>
            NamesMatch(tab.Descriptor.ServerName, server.Name) &&
            tab.Descriptor.Kind == ChatSessionKind.Channel &&
            tab.Descriptor.ChannelName.Length > 0);
    }


    private string BuildServerTreeSecondaryText(SavedServer server)
    {
        var session = _context.IrcRuntime.TryGetExistingSession(server, string.Empty);
        if (session == null)
        {
            return server.DisplayEndpoint;
        }

        if (session.IsConnected)
        {
            if (!string.IsNullOrWhiteSpace(session.Nickname))
            {
                return $"Connected as {session.Nickname}";
            }

            return "Connected";
        }

        return HasMeaningfulConnectionState(session.ConnectionState)
            ? session.ConnectionState
            : server.DisplayEndpoint;
    }

    private string BuildTargetTreeSecondaryText(SavedServer server, WorkspaceTarget target)
    {
        var session = _context.IrcRuntime.TryGetExistingSession(server, target.Name);
        var targetInfo = session == null ? _context.IrcRuntime.TryGetTargetInfo(server, target.Name) : null;

        if (NamesMatch(target.Name, "Server"))
        {
            if (session == null)
            {
                return string.Empty;
            }

            if (HasMeaningfulConnectionState(session.ConnectionState) && !session.IsConnected)
            {
                return session.ConnectionState;
            }

            return TrimSidebarSecondaryText(session.Motd);
        }

        if (session != null)
        {
            if (HasMeaningfulConnectionState(session.ConnectionState) && !session.IsConnected)
            {
                return session.ConnectionState;
            }

            if (target.IsDirectMessage)
            {
                return "Private message";
            }

            if (session.Participants.Count > 0)
            {
                return session.Participants.Count == 1 ? "1 user" : $"{session.Participants.Count} users";
            }

            var topicSnippet = TrimSidebarSecondaryText(session.Topic);
            if (!string.IsNullOrWhiteSpace(topicSnippet))
            {
                return topicSnippet;
            }
        }

        if (targetInfo != null)
        {
            if (HasMeaningfulConnectionState(targetInfo.ConnectionState) && !targetInfo.IsConnected)
            {
                return targetInfo.ConnectionState;
            }

            if (target.IsDirectMessage)
            {
                return "Private message";
            }

            if (targetInfo.ParticipantCount > 0)
            {
                return targetInfo.ParticipantCount == 1 ? "1 user" : $"{targetInfo.ParticipantCount} users";
            }

            var topicSnippet = TrimSidebarSecondaryText(targetInfo.Topic);
            if (!string.IsNullOrWhiteSpace(topicSnippet))
            {
                return topicSnippet;
            }
        }

        return string.Empty;
    }

    private static bool HasMeaningfulConnectionState(string? connectionState)
    {
        return !string.IsNullOrWhiteSpace(connectionState)
            && !string.Equals(connectionState, "Connected", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(connectionState, "Disconnected", StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimSidebarSecondaryText(string? value, int maxLength = 54)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var collapsed = string.Join(
            " ",
            value
                .Split(new[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => part.Length > 0));

        if (collapsed.Length <= maxLength)
        {
            return collapsed;
        }

        return collapsed[..(maxLength - 3)].TrimEnd() + "...";
    }

    private void WorkspaceSessions_TargetsChanged(string serverName)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            if (_activeServer != null && NamesMatch(_activeServer.Name, serverName))
            {
                RefreshWorkspaceTargets();
            }
            else
            {
                RebuildSidebarTree();
            }
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            if (_activeServer != null && NamesMatch(_activeServer.Name, serverName))
            {
                RefreshWorkspaceTargets();
            }
            else
            {
                RebuildSidebarTree();
            }
        });
    }

    private void UpdateSelectedSidebarSource()
    {
        var nextSource = SelectedTab?.Content as ISessionSidebarSource;
        if (ReferenceEquals(_selectedSidebarSource, nextSource))
        {
            RefreshSelectedSidebar();
            return;
        }

        DetachSelectedSidebarSource();
        _selectedSidebarSource = nextSource;

        if (_selectedSidebarSource != null)
        {
            _selectedSidebarSource.PropertyChanged += SelectedSidebarSource_PropertyChanged;
        }

        RefreshSelectedSidebar();
        OnPropertyChanged(nameof(CanToggleTopicBar));
    }

    private void DetachSelectedSidebarSource()
    {
        if (_selectedSidebarSource != null)
        {
            _selectedSidebarSource.PropertyChanged -= SelectedSidebarSource_PropertyChanged;
            _selectedSidebarSource = null;
        }
    }

    private void SelectedSidebarSource_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            RefreshSelectedSidebar();
            return;
        }

        DispatcherQueue.TryEnqueue(RefreshSelectedSidebar);
    }

    private void RefreshSelectedSidebar()
    {
        RefreshSidebarSummaries();
        OnPropertyChanged(nameof(SelectedSidebarTitle));
        OnPropertyChanged(nameof(SelectedSidebarSubtitle));
        OnPropertyChanged(nameof(SelectedSidebarSubtitleVisibility));
        OnPropertyChanged(nameof(SelectedSidebarStatus));
        OnPropertyChanged(nameof(SelectedSidebarStatusVisibility));
        OnPropertyChanged(nameof(SelectedSidebarPrimaryHeader));
        OnPropertyChanged(nameof(SelectedSidebarPrimaryItems));
        OnPropertyChanged(nameof(SelectedSidebarPrimaryVisibility));
        OnPropertyChanged(nameof(SelectedSidebarSecondaryHeader));
        OnPropertyChanged(nameof(SelectedSidebarSecondaryItems));
        OnPropertyChanged(nameof(SelectedSidebarSecondaryVisibility));
        OnPropertyChanged(nameof(CanActivateSelectedSidebarSecondaryItem));
    }

    private void RefreshSidebarSummaries()
    {
        foreach (var serverItem in SidebarTreeItems)
        {
            var server = FindServer(serverItem.Server.Name) ?? serverItem.Server;
            serverItem.Server = server;
            serverItem.SecondaryText = BuildServerTreeSecondaryText(server);
            ApplySidebarNotificationState(serverItem);

            foreach (var child in serverItem.Children)
            {
                if (child.Target == null)
                {
                    continue;
                }

                child.Server = server;
                child.SecondaryText = BuildTargetTreeSecondaryText(server, child.Target);
                ApplySidebarNotificationState(child);
            }
        }
    }

    private void SyncSidebarRuntimeSubscriptions()
    {
        var desiredSessions = new Dictionary<string, IrcRuntimeSessionState>(StringComparer.OrdinalIgnoreCase);

        foreach (var serverItem in SidebarTreeItems)
        {
            var server = FindServer(serverItem.Server.Name) ?? serverItem.Server;
            AddSidebarRuntimeSubscriptionCandidate(desiredSessions, server, string.Empty);

            foreach (var child in serverItem.Children)
            {
                if (child.Target == null)
                {
                    continue;
                }

                AddSidebarRuntimeSubscriptionCandidate(desiredSessions, server, child.Target.Name);
            }
        }

        foreach (var existing in _sidebarRuntimeSubscriptions.Keys.ToArray())
        {
            if (desiredSessions.TryGetValue(existing, out var desiredState)
                && ReferenceEquals(_sidebarRuntimeSubscriptions[existing].State, desiredState))
            {
                continue;
            }

            var subscription = _sidebarRuntimeSubscriptions[existing];
            subscription.State.PropertyChanged -= subscription.Handler;
            _sidebarRuntimeSubscriptions.Remove(existing);
        }

        foreach (var desired in desiredSessions)
        {
            if (_sidebarRuntimeSubscriptions.ContainsKey(desired.Key))
            {
                continue;
            }

            PropertyChangedEventHandler handler = SidebarRuntimeSession_PropertyChanged;
            desired.Value.PropertyChanged += handler;
            _sidebarRuntimeSubscriptions[desired.Key] = (desired.Value, handler);
        }
    }

    private void AddSidebarRuntimeSubscriptionCandidate(
        IDictionary<string, IrcRuntimeSessionState> desiredSessions,
        SavedServer server,
        string channelName)
    {
        var session = _context.IrcRuntime.TryGetExistingSession(server, channelName);
        if (session == null)
        {
            return;
        }

        desiredSessions[BuildSidebarRuntimeSubscriptionKey(server.Name, channelName)] = session;
    }

    private static string BuildSidebarRuntimeSubscriptionKey(string serverName, string channelName)
    {
        return serverName + "|" + channelName;
    }

    private void SidebarRuntimeSession_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IrcRuntimeSessionState.UnreadCount)
            && e.PropertyName != nameof(IrcRuntimeSessionState.HasUnreadMention)
            && !string.IsNullOrEmpty(e.PropertyName))
        {
            return;
        }

        if (DispatcherQueue == null || DispatcherQueue.HasThreadAccess)
        {
            RefreshSidebarSummaries();
            return;
        }

        _ = DispatcherQueue.TryEnqueue(RefreshSidebarSummaries);
    }

    private void SyncSidebarSelection()
    {
        if (_syncingSidebarSelection || SidebarTreeView == null)
        {
            return;
        }

        var targetItem = SelectedTab == null ? null : FindSidebarTreeItemForTab(SelectedTab);
        _syncingSidebarSelection = true;
        try
        {
            SidebarTreeView.SelectedItem = targetItem;
        }
        finally
        {
            _syncingSidebarSelection = false;
        }
    }

    private void BindTabContent(SessionTab tab)
    {
        if (tab.Content is not ChannelPage channelPage)
        {
            return;
        }

        channelPage.PropertyChanged += ChannelPage_PropertyChanged;
        UpdateTabNotificationState(tab);
    }

    private void UnbindTabContent(SessionTab tab)
    {
        if (tab.Content is not ChannelPage channelPage)
        {
            return;
        }

        channelPage.PropertyChanged -= ChannelPage_PropertyChanged;
    }

    private void ChannelPage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ChannelPage channelPage)
        {
            return;
        }

        if (e.PropertyName != nameof(ChannelPage.UnreadCount)
            && e.PropertyName != nameof(ChannelPage.HasUnreadMention)
            && !string.IsNullOrEmpty(e.PropertyName))
        {
            return;
        }

        if (DispatcherQueue == null || DispatcherQueue.HasThreadAccess)
        {
            UpdateTabNotificationStateForContent(channelPage);
            return;
        }

        _ = DispatcherQueue.TryEnqueue(() => UpdateTabNotificationStateForContent(channelPage));
    }

    private void UpdateTabNotificationStateForContent(ChannelPage channelPage)
    {
        var tab = Tabs.FirstOrDefault(candidate => ReferenceEquals(candidate.Content, channelPage));
        if (tab == null)
        {
            return;
        }

        UpdateTabNotificationState(tab);
    }

    private void UpdateTabNotificationState(SessionTab tab)
    {
        if (tab.Content is ChannelPage channelPage)
        {
            tab.UnreadCount = channelPage.UnreadCount;
            tab.HasMention = channelPage.HasUnreadMention;
        }
        else
        {
            tab.UnreadCount = 0;
            tab.HasMention = false;
        }

        UpdateSidebarNotificationState(tab);
    }

    private void SyncSessionActivityState()
    {
        foreach (var tab in Tabs)
        {
            if (tab.Content is ChannelPage channelPage)
            {
                channelPage.SetIsActiveView(ReferenceEquals(tab, SelectedTab));
                UpdateTabNotificationState(tab);
            }
        }
    }

    private void UpdateSidebarNotificationState(SessionTab tab)
    {
        var treeItem = FindSidebarTreeItemForTab(tab);
        if (treeItem == null)
        {
            return;
        }

        treeItem.UnreadCount = tab.UnreadCount;
        treeItem.HasMention = tab.HasMention;
    }

    private SidebarTreeItem? FindSidebarTreeItemForTab(SessionTab tab)
    {
        if (!string.IsNullOrWhiteSpace(tab.Descriptor.ServerName))
        {
            var serverItem = SidebarTreeItems.FirstOrDefault(item =>
                item.IsServer && NamesMatch(item.Server.Name, tab.Descriptor.ServerName));
            if (serverItem == null)
            {
                return null;
            }

            if (tab.Descriptor.Kind == ChatSessionKind.Server)
            {
                return serverItem;
            }

            if (!string.IsNullOrWhiteSpace(tab.Descriptor.ChannelName))
            {
                return serverItem.Children.FirstOrDefault(item =>
                    item.Target != null && NamesMatch(item.Target.Name, tab.Descriptor.ChannelName));
            }
        }

        return null;
    }

    private void ApplySidebarNotificationState(SidebarTreeItem item)
    {
        var tab = Tabs.FirstOrDefault(candidate =>
        {
            if (!NamesMatch(candidate.Descriptor.ServerName, item.Server.Name))
            {
                return false;
            }

            return item.IsServer
                ? candidate.Descriptor.Kind == ChatSessionKind.Server
                : item.Target != null && NamesMatch(candidate.Descriptor.ChannelName, item.Target.Name);
        });

        var runtimeSession = _context.IrcRuntime.TryGetExistingSession(
            item.Server,
            item.IsServer ? string.Empty : item.Target?.Name ?? string.Empty);

        item.UnreadCount = tab?.UnreadCount ?? runtimeSession?.UnreadCount ?? 0;
        item.HasMention = tab?.HasMention ?? runtimeSession?.HasUnreadMention ?? false;
    }

    private void LoadViewSettings()
    {
        IsTopicBarEnabled = _context.SettingsStore.GetBoolean(ConfigKeys.ShowTopicBar, true);
        IsSidebarEnabled = _context.SettingsStore.GetBoolean(ConfigKeys.ShowSidebar, true);
    }

    private void ApplyViewSettings()
    {
        foreach (var channelPage in Tabs.Select(tab => tab.Content).OfType<ChannelPage>())
        {
            channelPage.RefreshFromSettings();
        }

        if (RightRailColumn != null)
        {
            RightRailColumn.Width = IsSidebarEnabled ? new GridLength(240) : new GridLength(0);
        }

        if (RightSplitterColumn != null)
        {
            RightSplitterColumn.Width = IsSidebarEnabled ? new GridLength(4) : new GridLength(0);
        }

        if (RightRail != null)
        {
            RightRail.Visibility = SidebarVisibility;
        }

        if (TopicBarMenuItem != null)
        {
            TopicBarMenuItem.IsChecked = IsTopicBarEnabled;
        }

        if (SidebarMenuItem != null)
        {
            SidebarMenuItem.IsChecked = IsSidebarEnabled;
        }
    }

    private void ReleasePaneSplitter()
    {
        if (_activePaneSplitter == null)
        {
            return;
        }

        _activePaneSplitter.ReleasePointerCaptures();
        _activePaneSplitter = null;
    }

    private SavedServer? FindServer(string serverName)
    {
        return Servers.FirstOrDefault(server => NamesMatch(server.Name, serverName));
    }

    private int GetUserListClickMode()
    {
        return Math.Clamp(_context.SettingsStore.GetInt(ConfigKeys.UserListClick, 0), 0, 2);
    }

    private void ShowSidebarRosterMenu(
        FrameworkElement targetElement,
        Windows.Foundation.Point? offset,
        string selectedItem,
        SavedServer server,
        string channelName,
        bool isUser)
    {
        var normalizedItem = NormalizeRosterItem(selectedItem);
        if (normalizedItem.Length == 0)
        {
            return;
        }

        var menu = new MenuFlyout();
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = normalizedItem,
            IsEnabled = false
        });

        menu.Items.Add(new MenuFlyoutSeparator());

        if (isUser)
        {
            menu.Items.Add(CreateSidebarMenuItem("Mention User", () => _selectedSidebarSource?.InsertSidebarSecondaryItem(normalizedItem)));
            menu.Items.Add(CreateSidebarMenuItem("Send Message", () => OpenQueryAndFocus(server, normalizedItem)));
            menu.Items.Add(CreateSidebarMenuItem("Whois User", async () =>
                await _context.IrcRuntime.SendAsync(server, channelName, "/whois " + normalizedItem)));

            var operatorActions = new MenuFlyoutSubItem
            {
                Text = "Operator actions"
            };
            operatorActions.Items.Add(CreateSidebarMenuItem("OP User", async () =>
                await _context.IrcRuntime.SendAsync(server, channelName, "/op " + normalizedItem)));
            operatorActions.Items.Add(CreateSidebarMenuItem("Deop User", async () =>
                await _context.IrcRuntime.SendAsync(server, channelName, "/deop " + normalizedItem)));
            operatorActions.Items.Add(new MenuFlyoutSeparator());
            operatorActions.Items.Add(CreateSidebarMenuItem("Voice User", async () =>
                await _context.IrcRuntime.SendAsync(server, channelName, "/voice " + normalizedItem)));
            operatorActions.Items.Add(CreateSidebarMenuItem("Devoice User", async () =>
                await _context.IrcRuntime.SendAsync(server, channelName, "/devoice " + normalizedItem)));
            operatorActions.Items.Add(new MenuFlyoutSeparator());
            operatorActions.Items.Add(CreateSidebarMenuItem("Mute User", async () =>
                await _context.IrcRuntime.SendAsync(server, channelName, "/mute " + normalizedItem)));
            operatorActions.Items.Add(CreateSidebarMenuItem("Unmute User", async () =>
                await _context.IrcRuntime.SendAsync(server, channelName, "/unmute " + normalizedItem)));
            operatorActions.Items.Add(CreateSidebarMenuItem("Kick User", async () =>
                await _context.IrcRuntime.SendAsync(server, channelName, "/kick " + normalizedItem)));
            operatorActions.Items.Add(CreateSidebarMenuItem("Ban User", async () =>
            {
                await _context.IrcRuntime.SendAsync(server, channelName, "/ban " + normalizedItem);
                await _context.IrcRuntime.SendAsync(server, channelName, "/kick " + normalizedItem);
            }));
            menu.Items.Add(operatorActions);
        }
        else
        {
            menu.Items.Add(CreateSidebarMenuItem("Open Target", () => OpenChannelTab(server.Name, normalizedItem)));
        }

        if (offset.HasValue)
        {
            menu.ShowAt(targetElement, offset.Value);
        }
        else
        {
            menu.ShowAt(targetElement);
        }
    }

    private MenuFlyoutItem CreateSidebarMenuItem(string text, Func<Task> action)
    {
        var item = new MenuFlyoutItem
        {
            Text = text
        };
        item.Click += async (_, _) => await action();
        return item;
    }

    private MenuFlyoutItem CreateSidebarMenuItem(string text, Action action)
    {
        var item = new MenuFlyoutItem
        {
            Text = text
        };
        item.Click += (_, _) => action();
        return item;
    }

    private static bool TryGetDataContext<T>(DependencyObject? source, out T? value)
        where T : class
    {
        while (source != null)
        {
            if (source is FrameworkElement element && element.DataContext is T typed)
            {
                value = typed;
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        value = null;
        return false;
    }

    private Task OpenQueryAndFocus(SavedServer server, string nickname)
    {
        OpenChannelTab(server.Name, nickname);
        return _context.IrcRuntime.OpenChannelAsync(server, nickname);
    }

    private bool TryGetSidebarRosterContext(
        DependencyObject? originalSource,
        out string selectedItem,
        out SavedServer server,
        out string channelName,
        out bool isUser)
    {
        selectedItem = string.Empty;
        server = null!;
        channelName = string.Empty;
        isUser = false;

        if (SelectedTab?.Descriptor.ServerName is not { Length: > 0 } serverName)
        {
            return false;
        }

        server = FindServer(serverName)!;
        if (server == null)
        {
            return false;
        }

        channelName = SelectedTab.Descriptor.ChannelName;
        isUser = channelName.Length > 0 &&
                 string.Equals(SelectedSidebarSecondaryHeader, "Users", StringComparison.OrdinalIgnoreCase);

        if (originalSource == null)
        {
            return false;
        }

        var current = originalSource;
        while (current != null)
        {
            if (current is FrameworkElement { DataContext: string dataContext })
            {
                selectedItem = dataContext;
                return true;
            }

            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private static string NormalizeRosterItem(string item)
    {
        return item.TrimStart('~', '&', '@', '%', '+').Trim();
    }

    private void LogThemeState(string source)
    {
        try
        {
            var themeLogPath = System.IO.Path.Combine(StatePathResolver.GetStateDirectoryPath(), "theme.log");
            var directory = System.IO.Path.GetDirectoryName(themeLogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            var expectedTheme = _context.SettingsStore.GetBoolean(ConfigKeys.DarkTheme, true) ? "Dark" : "Light";
            var sessionTabs = ResolveSessionTabs();
            var leftRail = ResolveLeftRail();
            var rightRail = ResolveRightRail();
            var rootGrid = ResolveRootGrid();
            var message =
                $"{DateTimeOffset.Now:O} | {source} | Expected={expectedTheme} | PageRequested={RequestedTheme} | PageActual={ActualTheme} | RootBackground={DescribeBrush(rootGrid?.Background)} | LeftBackground={DescribeBrush(leftRail?.Background)} | RightBackground={DescribeBrush(rightRail?.Background)} | TabsResolved={sessionTabs != null} | Assembly={GetType().Assembly.Location}";
            System.IO.File.AppendAllLines(themeLogPath, new[] { message });
        }
        catch
        {
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private TabView? ResolveSessionTabs()
    {
        SessionTabs ??= FindDescendant<TabView>(this);
        return SessionTabs;
    }

    private Grid? ResolveRootGrid()
    {
        RootGrid ??= FindDescendant<Grid>(this);
        return RootGrid;
    }

    private Border? ResolveLeftRail()
    {
        LeftRail ??= FindBorders().FirstOrDefault();
        return LeftRail;
    }

    private Border? ResolveRightRail()
    {
        RightRail ??= FindBorders().Skip(1).FirstOrDefault();
        return RightRail;
    }

    private static string DescribeBrush(Brush? brush)
    {
        return brush switch
        {
            SolidColorBrush solidColorBrush => solidColorBrush.Color.ToString(),
            null => "(null)",
            _ => brush.GetType().Name
        };
    }

    private static bool NamesMatch(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private Border[] FindBorders()
    {
        return FindDescendants<Border>(this).Take(2).ToArray();
    }

    private void ApplyShellChrome(ElementTheme theme)
    {
        var pageBackground = theme == ElementTheme.Dark
            ? Color.FromArgb(0xFF, 0x11, 0x13, 0x15)
            : Color.FromArgb(0xFF, 0xF7, 0xF4, 0xEE);
        var railBackground = theme == ElementTheme.Dark
            ? Color.FromArgb(0xFF, 0x16, 0x16, 0x16)
            : Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3);
        var railBorder = theme == ElementTheme.Dark
            ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x1A, 0x00, 0x00, 0x00);

        var rootGrid = ResolveRootGrid();
        if (rootGrid != null)
        {
            rootGrid.RequestedTheme = theme;
            rootGrid.Background = new SolidColorBrush(pageBackground);
        }

        foreach (var rail in new[] { ResolveLeftRail(), ResolveRightRail() }.Where(border => border != null))
        {
            rail!.RequestedTheme = theme;
            rail.Background = new SolidColorBrush(railBackground);
            rail.BorderBrush = new SolidColorBrush(railBorder);
        }

        if (TitleBarHost != null)
        {
            TitleBarHost.RequestedTheme = theme;
            TitleBarHost.Background = new SolidColorBrush(railBackground);
            TitleBarHost.BorderBrush = new SolidColorBrush(railBorder);
        }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                return match;
            }

            var nestedMatch = FindDescendant<T>(child);
            if (nestedMatch != null)
            {
                return nestedMatch;
            }
        }

        return null;
    }

    private static System.Collections.Generic.IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nestedMatch in FindDescendants<T>(child))
            {
                yield return nestedMatch;
            }
        }
    }
}
