using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.UI;
using System.Globalization;
using System.Collections.ObjectModel;
using WinIRC.Net;
using Windows.UI.Popups;
using Windows.UI.Notifications;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinIRC.Commands;
using System.Diagnostics;
using WinIRC.Ui;
using System.Threading.Tasks;
using WinIRC.Views;
using WinIRC.Handlers;
using WinIRC.Utils;
using Windows.Storage;
using Windows.ApplicationModel.ExtendedExecution;
using Rymate.Controls.UWPMenuBar;
using Template10.Services.SerializationService;
using Windows.UI.Xaml.Data;
using WinIRC.Ui.Brushes;

namespace WinIRC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : INotifyPropertyChanged
    {
        private ObjectStorageHelper<ObservableCollection<string>> serversOSH;
        private ObjectStorageHelper<List<IrcServer>> serversListOSH;

        public string currentChannel { get; set; } = "";
        public string currentServer { get; set; } = "";

        public ObservableCollection<String> servers { get; set; }
        public List<IrcServer> serversList { get; set; }
        public bool SettingsLoaded = false;
        private bool loadedSavedServer;

        private ListView usersList;

        public string currentTopic { get; set; }

        public bool ShowTopic { get; set; } = true;

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

        public CommandHandler CommandHandler { get; private set; }

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
            this.CommandHandler = IrcHandler.CommandHandler;

            instance = this;
        }

        private void WindowStates_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            var sidebarColor = Config.GetBoolean(Config.DarkTheme) ? ParseColor("#FF000000") : ParseColor("#FFEEEEEE");
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


                UpdateUi();

                //ChannelFrame.Navigate(typeof(PlaceholderView)); // blank the frame
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("Error when loading saved servers: " + ex.Message);
                await dialog.ShowAsync();
            }

            Servers.ItemsSource = IrcServers.Instance.Servers;
            var cvs = (CollectionViewSource)Resources["channelsSrc"];
            cvs.Source = IrcHandler.Servers;

            if (e.Parameter != null)
            {
                var serv = SerializationService.Json;

                var launchEvent = serv.Deserialize<String>(e.Parameter as String);
                this.ConnectViaName(launchEvent);
            }

        }

        public void ConnectViaName(string args)
        {
            if (args == "") return; 

            var server = IrcServers.Instance.Get(args);
            Connect(IrcServers.Instance.CreateConnection(server));
        }

        private void MenuBarItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as MenuBarItem;

            var server = button.DataContext as IrcServer;
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

            Window.Current.SetTitleBar(Menu.DragArea);

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
            Menu.Foreground = new SolidColorBrush(ParseColor("#FFFFFFFF"));
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

                this.ListBoxItemStyle = style;
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

            var sidebarColor = Config.GetBoolean(Config.DarkTheme) ? ParseColor("#FF000000") : ParseColor("#FFEEEEEE");
            if (Config.GetBoolean(Config.Blurred, true))
            {
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                {
                    var source = WindowStates.CurrentState == WideState ? AcrylicBackgroundSource.HostBackdrop : AcrylicBackgroundSource.Backdrop;
                    var brush = new Windows.UI.Xaml.Media.AcrylicBrush
                    {
                        FallbackColor = sidebarColor,
                        BackgroundSource = source,
                        TintColor = sidebarColor,
                        TintOpacity = 0.75
                    };

                    SplitView.PaneBackground = brush;
                }
                else if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
                {
                    var backdrop = WindowStates.CurrentState == WideState;
                    AcrylicBrushBase brush = backdrop ? (AcrylicBrushBase) new AcrylicHostBrush() : new Ui.Brushes.AcrylicBrush();
                    brush.TintColor = sidebarColor;
                    brush.BlurAmount = backdrop ? 5 : 15; 
                    brush.BackdropFactor = backdrop ? 0.2 : 1;

                    SplitView.PaneBackground = brush;
                }
            }
            else
            {
                SplitView.PaneBackground = new SolidColorBrush(sidebarColor);
            }

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
            GetCurrentChannelView().ScrollToBottom();
        }

        private void ChannelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (channelList.SelectedItem == null)
                    return;

                var channel = channelList.SelectedItem.ToString();
                currentServer = ((Channel) channelList.SelectedItem).Server;
                SwitchChannel(currentServer, channel, false);
                IrcHandler.UpdateUsers(SidebarFrame, currentServer, channel);
                GetCurrentChannelView().ScrollToBottom();
            }
            catch (Exception ex)
            {
                var toast = Irc.CreateBasicToast(ex.Message, ex.StackTrace);
                toast.ExpirationTime = DateTime.Now.AddDays(2);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
        }

        public void SwitchChannel(string server, string channel, bool auto)
        {
            //ChannelFrame.Navigate(typeof(ChannelView), new string[] { server, channel });
            SidebarHeader.Title = "Channel Users";

            if ((auto || lastAuto || !Config.GetBoolean(Config.UseTabs)) && (GetCurrentItem() != null))
            {
                if (auto != lastAuto) IrcHandler.connectedServers[currentServer].channelStore[channel].SortUsers();

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
                Tabs.SelectedItem = Tabs.Items.Cast<PivotItem>().First(item => item.Header as string == channel);
                IrcHandler.connectedServers[currentServer].channelStore[channel].SortUsers();
            }
            else
            {
                CreateNewTab(server, channel);
                IrcHandler.connectedServers[currentServer].channelStore[channel].SortUsers();
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

        public Irc GetCurrentServer()
        {
            try
            {
                return IrcHandler.connectedServers[currentServer];
            } catch
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

        public async void Connect(Irc irc)
        {
            if (IrcHandler.connectedServersList.Contains(irc.server.name)) return;
            if (IrcHandler.connectedServersList.Contains(irc.server.hostname)) return;

            irc.HandleDisconnect += HandleDisconnect;

            // connect
            if (Tabs.Items.Count != 0)
            {
                if ((Tabs.Items[0] as PivotItem).Content is PlaceholderView) Tabs.Items.RemoveAt(0);
            }

            irc.Connect();

            // link the server up to the lists
            IrcHandler.connectedServers.Add(irc.server.name, irc);
            IrcHandler.connectedServersList.Add(irc.server.name);
            currentServer = irc.server.name;

            if (Config.GetBoolean(Config.UseTabs)) CreateNewTab(irc.server.name, "Server");
            lastAuto = Config.GetBoolean(Config.UseTabs);
        }

        public async void HandleDisconnect(Irc irc)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (IrcHandler.connectedServersList.Count > 1)
                {
                    currentServer = IrcHandler.connectedServersList[0];
                    //channelList.ItemsSource = IrcHandler.connectedServers.Values.First().channelList;
                }

                foreach (var buffer in IrcHandler.connectedServers[irc.server.name].channelBuffers)
                {
                    buffer.Value.Clear();
                }

                var name = irc.server.name;

                IrcHandler.connectedServers[irc.server.name].channelBuffers.Clear();
                IrcHandler.connectedServers[irc.server.name].channelList.Clear();
                IrcHandler.connectedServers[irc.server.name].channelStore.Clear();

                IrcHandler.connectedServers.Remove(irc.server.name);
                IrcHandler.connectedServersList.Remove(irc.server.name);
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

            CommandHandler.PartCommandHandler(GetCurrentServer(), new string[] { "PART ", channel });
        }

        private void ChannelListItem_ChannelJoinClicked(object sender, EventArgs e)
        {
            var channelArgs = e as ChannelEventArgs;
            var channel = channelArgs.Channel;

            GetCurrentServer().JoinChannel(channel);
        }

        internal void CloseConnectView()
        {
            serverConnect.IsModal = !serverConnect.IsModal;
        }

        public async void IrcPrompt(IrcServer server)
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

            server.username = Config.GetString(Config.DefaultUsername);
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
            var header = sender as ChannelListItem;

            SwitchChannel(header.Title, "Server", false);
        }
    }
}
