using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.UI;
using System.Globalization;
using System.Collections.ObjectModel;
using Windows.UI.Popups;
using Windows.UI.Notifications;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using WinIRC.Ui;
using WinIRC.Views;
using WinIRC.Handlers;
using WinIRC.Utils;
using Template10.Services.SerializationService;
using Windows.UI.Xaml.Data;
using WinIRC.Net;
using IrcClientCore;
using WinIrcServer = WinIRC.Net.WinIrcServer;
using IrcClientCore.Handlers.BuiltIn;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Hosting;
using Microsoft.AppCenter.Analytics;

namespace WinIRC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : INotifyPropertyChanged
    {
        public string currentChannel { get; set; } = "";
        public string currentServer { get; set; } = "";

        public ObservableCollection<String> servers { get; set; }
        public List<WinIrcServer> serversList { get; set; }
        public bool SettingsLoaded = false;

        public string currentTopic { get; set; }

        public bool ShowTopic { get; set; } = true;

        public event EventHandler UiUpdated;

        public Visibility _TabsVisible = Visibility.Visible;
        public Visibility TabsVisible
        {
            get => _TabsVisible;
            set
            {
                _TabsVisible = value;
                NotifyPropertyChanged();
            }
        }

        public bool _ShowingUsers;

        public bool ShowingUsers
        {
            get => _ShowingUsers;
            set
            {
                if (currentChannel == "" || currentServer == "" || !IrcHandler.connectedServers.ContainsKey(currentServer))
                {
                    this.NotifyPropertyChanged();
                    return;
                }

                SidebarFrame.BackStack.Clear();
                SidebarHeader.ShowBackButton = false;

                ShouldPin();

                if (value)
                {
                    IrcHandler.UpdateUsers(SidebarFrame, currentServer, currentChannel);
                    UpdateInfo(currentServer, currentChannel);
                    SidebarHeader.Title = "Channel Users";
                    SidebarSplitView.IsPaneOpen = true;
                }

                _ShowingUsers = SidebarFrame.Content is UsersView && SidebarSplitView.IsPaneOpen;
                this.NotifyPropertyChanged();
            }
        }

        private Style _ListBoxItemStyle;
        public Style ListBoxItemStyle
        {
            get { return this._ListBoxItemStyle; }

            set
            {
                if (value == this._ListBoxItemStyle) return;
                this._ListBoxItemStyle = value;
                NotifyPropertyChanged();
            }
        }

        internal IrcUiHandler IrcHandler { get; private set; }

        public static MainPage instance;
        private bool lastAuto;


        private SolidColorBrush _AccentColor;

        public SolidColorBrush AccentColor
        {
            get
            {
                return _AccentColor;
            }
            set
            {
                this._AccentColor = value;
                NotifyPropertyChanged();
            }
        }

        private SolidColorBrush _AccentColorAlt;

        public SolidColorBrush AccentColorAlt
        {
            get
            {
                return _AccentColorAlt;
            }
            set
            {
                this._AccentColorAlt = value;
                NotifyPropertyChanged();
            }
        }

        public MainPage()
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            AccentColor = new SolidColorBrush(uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent));
            AccentColorAlt = new SolidColorBrush(uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentDark1));

            this.InitializeComponent();

            this.IrcHandler = new IrcUiHandler();

            this.LoadSettings();

            this.DataContext = IrcHandler;

            currentChannel = "";
            currentServer = "";
            currentTopic = "";

            var inputPane = InputPane.GetForCurrentView();
            inputPane.Showing += this.InputPaneShowing;
            inputPane.Hiding += this.InputPaneHiding;

            Window.Current.SizeChanged += Current_SizeChanged;
            SidebarFrame.Navigated += SidebarFrame_Navigated;
            WindowStates.CurrentStateChanged += WindowStates_CurrentStateChanged;
            WindowStates.CurrentStateChanging += WindowStates_CurrentStateChanging; ;

            this.ListBoxItemStyle = Application.Current.Resources["ListBoxItemStyle"] as Style;

            instance = this;
        }

        private void WindowStates_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            var sidebarColor = Config.GetBoolean(Config.DarkTheme) ? ParseColor("#FF111111") : ParseColor("#FFEEEEEE");
            SplitView.PaneBackground = new SolidColorBrush(sidebarColor);
        }

        private void WindowStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            UpdateUi();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                CreateNewTab("Welcome");

                if (!Config.Contains(Config.DefaultUsername) || Config.GetString(Config.DefaultUsername) == "")
                {
                    var dialog = new PromptDialog
                    (
                        title: "Set default username",
                        text: "WinIRC allows you to set a default username now, please set it below. This can be changed later",
                        placeholder: "Username",
                        confirmButton: "Set",
                        def: "winircuser-" + (new Random()).Next(100, 1000)
                    );

                    var result = await dialog.Show();

                    if (result == ContentDialogResult.Primary)
                    {
                        Config.SetString(Config.DefaultUsername, dialog.Result);
                    }
                    else
                    {
                        Config.SetString(Config.DefaultUsername, "");
                    }
                }

                if (!Config.Contains(Config.AnalyticsAsked))
                {
                    AnalyticsPopup.IsOpen = true;
                }

                UpdateUi();

                //ChannelFrame.Navigate(typeof(PlaceholderView)); // blank the frame
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("Error when loading saved servers: " + ex.Message);
                await dialog.ShowAsync();
            }

            IrcServers.Instance.Servers.CollectionChanged += (sender, ex) => RefreshSubMenu();

            RefreshSubMenu();

            var cvs = (CollectionViewSource)Resources["channelsSrc"];
            cvs.Source = IrcHandler.Servers;

            if (e.Parameter != null)
            {
                var serv = SerializationService.Json;

                var launchEvent = serv.Deserialize<String>(e.Parameter as String);
                this.ConnectViaName(launchEvent);
            }

            if (ApiInformation.IsTypePresent("Windows.UI.WindowManagement.AppWindow"))
            {
                openWindowButton.Visibility = Visibility.Visible;
                openWindowButton.Click += OpenWindowButton_Click;
            }
        }

        private void RefreshSubMenu()
        {
            var items = ConnectSubMenu.Items;

            items.Clear();

            foreach (var server in IrcServers.Instance.Servers)
            {
                var item = new MenuFlyoutItem()
                {
                    Text = server.Name,
                    DataContext = server 
                };

                item.Click += MenuBarItem_Click;

                items.Add(item);
            }
        }

        private async void OpenWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var window = await AppWindow.TryCreateAsync();
            var currentView = GetCurrentChannelView();

            if (currentView != null)
            {
                ChannelView view = new ChannelView(currentView.currentServer, currentView.currentChannel, window);
                ElementCompositionPreview.SetAppWindowContent(window, view);
                window.Title = $"{currentView.currentChannel} | {currentView.currentServer}";
                window.TitleBar.ExtendsContentIntoTitleBar = true;
                window.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                CloseTab_Click(sender, e);
            }

            await window.TryShowAsync();
        }

        public void ConnectViaName(string args)
        {
            if (args == "") return;

            var server = IrcServers.Instance.Get(args);
            Connect(IrcServers.Instance.CreateConnection(server));
        }

        private void MenuBarItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as MenuFlyoutItem;

            var server = button.DataContext as Net.WinIrcServer;
            Connect(IrcServers.Instance.CreateConnection(server));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadSettings()
        {
            if (Config.Contains(Config.DarkTheme))
            {
                var darktheme = Config.GetBoolean(Config.DarkTheme);
                this.RequestedTheme = darktheme ? ElementTheme.Dark : ElementTheme.Light;
            }
            else
            {
                Config.SetBoolean(Config.DarkTheme, true);
                this.RequestedTheme = ElementTheme.Dark;
            }

            if (!Config.Contains(Config.UseTabs))
            {
                Config.SetBoolean(Config.UseTabs, true);
            }

            ManageTitleBar();
            SettingsLoaded = true;
        }

        public void ManageTitleBar()
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            AccentColor = new SolidColorBrush(uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent));

            AccentColorAlt = new SolidColorBrush(uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentDark1));

            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            Window.Current.SetTitleBar(TitleBarPadding);

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            var darkTheme = Config.GetBoolean(Config.DarkTheme);

            var background = ParseColor("#FF1F1F1F");
            var backgroundInactive = ParseColor("#FF2B2B2B");
            var foreground = ParseColor("#FFFFFFFF");

            titleBar.BackgroundColor = _AccentColor.Color;
            titleBar.InactiveBackgroundColor = backgroundInactive;
            titleBar.ButtonHoverBackgroundColor = AccentColorAlt.Color;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = AccentColorAlt.Color;
            titleBar.ButtonForegroundColor = foreground;

            Menu.Background = AccentColor;
        }


        private Color ParseColor(string hexCode)
        {
            var color = new Color();
            color.A = byte.Parse(hexCode.Substring(1, 2), NumberStyles.AllowHexSpecifier);
            color.R = byte.Parse(hexCode.Substring(3, 2), NumberStyles.AllowHexSpecifier);
            color.G = byte.Parse(hexCode.Substring(5, 2), NumberStyles.AllowHexSpecifier);
            color.B = byte.Parse(hexCode.Substring(7, 2), NumberStyles.AllowHexSpecifier);
            return color;
        }

        internal void UpdateUi()
        {
            if (Config.Contains(Config.ReducedPadding))
            {
                int height;

                if (Config.GetBoolean(Config.ReducedPadding))
                {
                    height = 28;
                }
                else
                {
                    height = 42;
                }

                var res = new ResourceDictionary { Source = new Uri("ms-appx:///Styles/Styles.xaml", UriKind.Absolute) };

                var style = res["ListBoxItemStyle"] as Style;

                foreach (var item in style.Setters.Cast<Setter>().Where(item => item.Property == HeightProperty))
                    style.Setters.Remove(item);

                style.Setters.Add(new Setter(HeightProperty, height));

                this.channelList.ItemContainerStyle = style;
            }

            if (Config.Contains(Config.HideStatusBar))
            {
                if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
                {
                    StatusBar statusBar = StatusBar.GetForCurrentView();
                    if (Config.GetBoolean(Config.HideStatusBar))
                        statusBar.HideAsync();
                    else
                        statusBar.ShowAsync();
                }
            }

            var sidebarColor = Config.GetBoolean(Config.DarkTheme) ? ParseColor("#FF111111") : ParseColor("#FFEEEEEE");
            Brush brush;
            try
            {
                if (Config.GetBoolean(Config.Blurred, true) && ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.AcrylicBrush"))
                {
                    var hostBackdrop = WindowStates.CurrentState == WideState;
                    var source = hostBackdrop ? Microsoft.UI.Xaml.Media.AcrylicBackgroundSource.HostBackdrop : Microsoft.UI.Xaml.Media.AcrylicBackgroundSource.Backdrop;
                    brush = new Microsoft.UI.Xaml.Media.AcrylicBrush
                    {
                        FallbackColor = sidebarColor,
                        BackgroundSource = source,
                        TintColor = sidebarColor,
                        TintOpacity = hostBackdrop ? 0.75 : 0.55
                    };
                }
                else
                {
                    brush = new SolidColorBrush(sidebarColor);
                }
            }
            catch (Exception e)
            {
                brush = new SolidColorBrush(sidebarColor);
            }

            SplitView.PaneBackground = brush;

            if (Config.Contains(Config.UseTabs))
            {
                TabsVisible = Config.GetBoolean(Config.UseTabs) ? Visibility.Visible : Visibility.Collapsed;
                HeaderColor.Visibility = (Config.GetBoolean(Config.UseTabs) || ShowTopic) ? Visibility.Visible : Visibility.Collapsed;
            }

            foreach (PivotItem item in Tabs.Items.Cast<PivotItem>())
            {
                if (item.Content is ChannelView)
                {
                    var view = item.Content as ChannelView;

                    view.UpdateUi();
                }
            }

            UiUpdated?.Invoke(this, new EventArgs());
        }

        public PivotItem GetCurrentItem()
        {
            return Tabs.SelectedItem as PivotItem;
        }

        public ChannelView GetCurrentChannelView()
        {
            if (GetCurrentItem() == null) return null;

            var item = GetCurrentItem().Content as ChannelView;
            return item;
        }

        public TextBox GetInputBox()
        {
            if (GetCurrentChannelView() != null)
                return GetCurrentChannelView().GetInputBox();
            else return null;
        }

        public ListView GetChannelList()
        {
            return channelList;
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            var bounds = Window.Current.Bounds;
            double height = bounds.Height;
            connectDialogRoot.MaxHeight = height;
        }

        private void ConnectDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(ConnectFrame.Content is ConnectView))
                ConnectFrame.Navigate(typeof(ConnectView));

            var bounds = Window.Current.Bounds;
            double height = bounds.Height;
            connectDialogRoot.MaxHeight = height;
        }

        private void InputPaneHiding(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            this.mainGrid.Margin = new Thickness();
        }

        private void InputPaneShowing(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            if (GetInputBox() == null) return;

            if (GetInputBox().FocusState != FocusState.Unfocused)
            {
                this.mainGrid.Margin = new Thickness(0, -70, 0, args.OccludedRect.Height);
                args.EnsuredFocusedElementInView = true;
            }
        }

        private void ChannelList_ItemClick(object sender, ItemClickEventArgs e)
        {
            channelList.SelectedItem = e.ClickedItem;
            try
            {
                if (channelList.SelectedItem == null)
                    return;

                var channel = channelList.SelectedItem.ToString();
                currentServer = ((Channel)channelList.SelectedItem).Server;
                SwitchChannel(currentServer, channel, false);
                IrcHandler.UpdateUsers(SidebarFrame, currentServer, channel);
            }
            catch (Exception ex)
            {
                var toast = IrcUWPBase.CreateBasicToast(ex.Message, ex.StackTrace);
                toast.ExpirationTime = DateTime.Now.AddDays(2);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
                Debug.WriteLine(ex);
            }
        }

        public void SwitchChannel(string server, string channel, bool auto)
        {
            //ChannelFrame.Navigate(typeof(ChannelView), new string[] { server, channel });
            SidebarHeader.Title = "Channel Users";

            if (channel == "Server")
            {
                if (Tabs.Items.Cast<PivotItem>().Any(item => item.Header as string == channel))
                {
                    Tabs.SelectedItem = Tabs.Items.Cast<PivotItem>().First(item => item.Header as string == channel);
                }
                else
                {
                    CreateNewTab(server, channel);
                }
                return;
            }

            if ((auto || lastAuto || !Config.GetBoolean(Config.UseTabs)) && (GetCurrentItem() != null))
            {
                var item = GetCurrentItem();
                lastAuto = auto;

                item.Header = channel;

                if (item.Content is ChannelView)
                {
                    (item.Content as ChannelView).SetChannel(server, channel);
                }
                else
                {
                    item.Content = new ChannelView(server, channel);
                }
            }
            else if (Tabs.Items.Cast<PivotItem>().Any(item => item.Header as string == channel))
            {
                Tabs.SelectedItem = Tabs.Items.Cast<PivotItem>().First(item =>
                    item.Header as string == channel
                    && (item.Content as ChannelView).currentServer == server
                );
            }
            else
            {
                CreateNewTab(server, channel);
            }

            UpdateInfo(server, channel);
        }

        private PlaceholderView CreateNewTab(String header)
        {
            PivotItem p = new PivotItem();
            p.Header = header;
            PlaceholderView view = new PlaceholderView();

            p.Margin = new Thickness(0, 0, 0, -2);

            p.Content = view;

            Tabs.Items.Add(p);

            Tabs.SelectedItem = p;

            return view;
        }

        private ChannelView CreateNewTab(String server, String channel)
        {
            PivotItem p = new PivotItem();
            p.Header = channel;

            ChannelView view = new ChannelView(server, channel);

            p.Margin = new Thickness(0, 0, 0, -2);

            p.Content = view;

            Tabs.Items.Add(p);

            Tabs.SelectedItem = p;

            return view;
        }

        internal ChannelListView ShowChannelsList(List<IrcClientCore.Handlers.BuiltIn.ChannelListItem> obj)
        {
            PivotItem p = new PivotItem();
            p.Header = "Channels";
            ChannelListView view = new ChannelListView(obj);

            p.Margin = new Thickness(0, 0, 0, -2);

            p.Content = view;

            Tabs.Items.Add(p);

            Tabs.SelectedItem = p;

            return view;
        }


        public void UpdateInfo(string server, string channel)
        {
            if (currentServer != server)
            {
                currentServer = server;
            }

            if (IrcHandler.connectedServers.ContainsKey(currentServer))
            {
                IrcHandler.connectedServers[currentServer].SwitchChannel(channel);
            }

            currentChannel = channel;

            if (SplitView.DisplayMode == SplitViewDisplayMode.Overlay)
                SplitView.IsPaneOpen = false;

            channelList.SelectedValue = channel;
            IrcHandler.UpdateUsers(SidebarFrame, currentServer, currentChannel);
        }

        public IrcUWPBase GetCurrentServer()
        {
            try
            {
                return IrcHandler.connectedServers[currentServer];
            }
            catch
            {
                return null;
            }
        }

        public IrcUWPBase GetServer(string server)
        {
            try
            {
                return IrcHandler.connectedServers[server];
            }
            catch
            {
                return null;
            }
        }

        public void MentionReply(string ircserver, string channel, string message)
        {
            IrcHandler.connectedServers[ircserver].SendMessage(channel, message);
        }

        private void ToggleSplitView(object sender, RoutedEventArgs e)
        {
            SplitView.IsPaneOpen = !SplitView.IsPaneOpen;
        }

        private void ShowConnectPopup(object sender, RoutedEventArgs e)
        {
            serverConnect.IsModal = !serverConnect.IsModal;
        }

        public async void Connect(IrcUWPBase irc)
        {
            if (IrcHandler.connectedServersList.Contains(irc.Server.Name)) return;
            if (IrcHandler.connectedServersList.Contains(irc.Server.Hostname)) return;

            irc.HandleDisconnect += HandleDisconnect;
            irc.Initialise();

            // connect
            if (Tabs.Items.Count != 0)
            {
                if ((Tabs.Items[0] as PivotItem).Content is PlaceholderView) Tabs.Items.RemoveAt(0);
            }

            irc.Connect();

            // link the server up to the lists
            IrcHandler.connectedServers.Add(irc.Server.Name, irc);
            IrcHandler.connectedServersList.Add(irc.Server.Name);
            currentServer = irc.Server.Name;

            if (Config.GetBoolean(Config.UseTabs)) CreateNewTab(irc.Server.Name, "Server");
            lastAuto = Config.GetBoolean(Config.UseTabs);
        }

        public async void HandleDisconnect(IrcUWPBase irc)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (IrcHandler.connectedServersList.Count > 1)
                {
                    currentServer = IrcHandler.connectedServersList[0];
                    //channelList.ItemsSource = IrcHandler.connectedServers.Values.First().channelList;
                }

                foreach (var channel in IrcHandler.connectedServers[irc.Server.Name].ChannelList)
                {
                    channel.Buffers.Clear();
                }

                var name = irc.Server.Name;

                IrcHandler.connectedServers[irc.Server.Name].ChannelList.Clear();

                IrcHandler.connectedServers.Remove(irc.Server.Name);
                IrcHandler.connectedServersList.Remove(irc.Server.Name);
                irc.HandleDisconnect = null;
                irc.ConnCheck.ConnectionChanged = null;
                irc.ConnCheck = null;
                irc.Dispose();

                List<PivotItem> Temp = new List<PivotItem>();
                Debug.WriteLine("All tabs: " + Tabs.Items.Count);

                var count = Tabs.Items.Count;

                for (var i = 0; i < count; i++)
                {
                    Debug.WriteLine("Tabs seen: " + i);
                    var item = Tabs.Items[0] as PivotItem;
                    var content = item.Content;
                    if (content is ChannelView && (content as ChannelView).currentServer == name)
                    {
                        item.Content = null;
                        Tabs.Items.Remove(item);
                    }
                }

                Debug.WriteLine(Tabs.Items.Count);

                if (Tabs.Items.Count == 0)
                {
                    PivotItem p = new PivotItem();
                    lastAuto = true;
                    p.Header = "Welcome";
                    Frame frame = new Frame();

                    p.Margin = new Thickness(0, 0, 0, -2);

                    p.Content = frame;
                    Tabs.Items.Add(p);
                    Tabs.SelectedIndex = Tabs.Items.Count - 1;
                    frame.Navigate(typeof(PlaceholderView));
                }
            });
        }

        private void AppearanceSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings(typeof(DisplaySettingsView));
        }

        private void AboutPage_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings(typeof(AboutView));
        }

        private void ShowSettings(Type type)
        {
            SidebarFrame.BackStack.Clear();
            SidebarHeader.ShowBackButton = false;

            ShowingUsers = false;

            if (SidebarFrame.Content == null || !(SidebarFrame.Content.GetType() == type))
            {
                SidebarFrame.Navigate(type);
                if (SidebarFrame.Content is BaseSettingsPage)
                {
                    var settingsView = (BaseSettingsPage)SidebarFrame.Content;

                    if (settingsView != null)
                        SidebarHeader.Title = settingsView.Title;
                }
                else if (type.Name == nameof(AboutView))
                {
                    SidebarHeader.Title = "About";
                }
            }

            NotifyPropertyChanged(nameof(ShowingUsers));
            ToggleSidebar();
        }

        private void BehaviourSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings(typeof(BehaviourSettingsView));
        }

        private void PinSidebar(object sender, RoutedEventArgs e)
        {
            if (!SidebarPinned())
            {
                SidebarSplitView.DisplayMode = SplitViewDisplayMode.Inline;
            }
            else
            {
                SidebarSplitView.DisplayMode = SplitViewDisplayMode.Overlay;
                SidebarSplitView.IsPaneOpen = false;
            }

            ShowingUsers = false;
        }

        private Boolean SidebarPinned()
        {
            return SidebarSplitView.DisplayMode == SplitViewDisplayMode.Inline;
        }

        private void ToggleSidebar()
        {
            ShouldPin();

            if (!SidebarPinned() || (SidebarPinned() && !SidebarSplitView.IsPaneOpen))
            {
                SidebarSplitView.IsPaneOpen = !SidebarSplitView.IsPaneOpen;
            }
        }

        private void ShouldPin()
        {
            if (WindowStates.CurrentState == WideState)
            {
                SidebarSplitView.DisplayMode = SplitViewDisplayMode.Inline;
            }
            else
            {
                SidebarSplitView.DisplayMode = SplitViewDisplayMode.Overlay;
            }
        }


        private void HeaderBlock_BackButtonClicked(object sender, EventArgs e)
        {
            if (SidebarFrame.CanGoBack)
                SidebarFrame.GoBack();

            if (SidebarFrame.Content is SettingsView)
            {
                var settingsView = (SettingsView)SidebarFrame.Content;

                if (settingsView != null)
                {
                    SidebarHeader.Title = "Settings";
                    settingsView.Header = SidebarHeader;
                }
            }

            SidebarHeader.ShowBackButton = false;

        }

        private void SidebarFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (SidebarFrame.Content is BaseSettingsPage)
            {
                ((BaseSettingsPage)SidebarFrame.Content).UpdateUi += UpdateUi;
            }
        }

        private void ChannelListItem_ChannelCloseClicked(object sender, EventArgs e)
        {
            var channelArgs = e as ChannelEventArgs;
            var channel = channelArgs.Channel;

            GetServer(channelArgs.Server).PartChannel(channel);
        }

        private void ChannelListItem_ChannelJoinClicked(object sender, EventArgs e)
        {
            var channelArgs = e as ChannelEventArgs;
            var channel = channelArgs.Channel;

            GetServer(channelArgs.Server).JoinChannel(channel);
        }

        internal void CloseConnectView()
        {
            serverConnect.IsModal = !serverConnect.IsModal;
        }

        public async void IrcPrompt(WinIrcServer server)
        {
            if (!Config.Contains(Config.DefaultUsername) || Config.GetString(Config.DefaultUsername) == "")
            {
                var dialog = new PromptDialog
                (
                    title: "Set a username",
                    text: "To connect to irc servers, enter in a username first.",
                    placeholder: "Username",
                    confirmButton: "Set",
                    def: "winircuser-" + (new Random()).Next(100, 1000)
                );

                var result = await dialog.Show();

                if (result == ContentDialogResult.Primary)
                {
                    Config.SetString(Config.DefaultUsername, dialog.Result);
                }
            }

            server.Username = Config.GetString(Config.DefaultUsername);
            var irc = new Net.IrcSocket(server);
            MainPage.instance.Connect(irc);
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (GetCurrentItem() != null)
            {
                GetCurrentItem().Content = null;
                Tabs.Items.Remove(GetCurrentItem());
            }
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GetCurrentChannelView() != null)
                UpdateInfo(GetCurrentChannelView().currentServer, GetCurrentChannelView().currentChannel);
        }

        private void ChannelListItem_ServerRightClickEvent(object sender, EventArgs e)
        {
            var args = e as ServerRightClickArgs;
            var server = IrcHandler.connectedServers[args.server];

            if (args.type == ServerRightClickType.RECONNECT)
                server.DisconnectAsync(attemptReconnect: true);
            else if (args.type == ServerRightClickType.CLOSE)
                server.DisconnectAsync(attemptReconnect: false);
        }

        private void MenuBarToggleItem_Click(object sender, RoutedEventArgs e)
        {
            UpdateUi();
        }

        private void SidebarSplitView_PaneClosed(SplitView sender, object args)
        {
            ShowingUsers = false;
        }

        private void ConnectionSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings(typeof(ConnectionSettingsView));
        }

        private void ChannelListItem_ServerClickEvent(object sender, EventArgs e)
        {
            var header = sender as Ui.ChannelListItem;

            SwitchChannel(header.Server, "Server", false);
        }

        private void ListChannels_Click(object sender, RoutedEventArgs e)
        {
            GetCurrentServer()?.CommandManager.HandleCommand(currentChannel, "/list");
        }
        public void ShowTeachingTip()
        {
            if (Config.GetBoolean(Config.HideBackgroundTip))
            {
                return;
            }

            BackgroundTeachingTip.IsOpen = true;
        }

        private void BackgroundTeachingTip_ActionButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
        {
            Config.SetBoolean(Config.HideBackgroundTip, true);

            BackgroundTeachingTip.IsOpen = false;
        }

        private void AnalyticsPopup_ActionButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
        {
            Config.SetBoolean(Config.AnalyticsAsked, true);

            Analytics.SetEnabledAsync(false);
            AnalyticsPopup.IsOpen = false;
        }

        private void AnalyticsPopup_Closed(Microsoft.UI.Xaml.Controls.TeachingTip sender, Microsoft.UI.Xaml.Controls.TeachingTipClosedEventArgs args)
        {
            Config.SetBoolean(Config.AnalyticsAsked, true);
        }
    }
}
