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
using WinRtUtility;
using WinIRC.Net;
using Windows.UI.Popups;
using Windows.UI.Notifications;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinIRC.Commands;
using System.Diagnostics;
using WinIRC.Ui;
using System.Threading.Tasks;

namespace WinIRC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : INotifyPropertyChanged
    {
        private ObjectStorageHelper<ObservableCollection<string>> serversOSH;
        private ObjectStorageHelper<List<IrcServer>> serversListOSH;

        public string currentChannel { get; set; }
        public string currentServer { get; set; }
        public Dictionary<string, Net.Irc> connectedServers { get; set; }
        public ObservableCollection<String> connectedServersList { get; set; }

        public ObservableCollection<String> servers { get; set; }
        public List<Net.IrcServer> serversList { get; set; }
        public bool SettingsLoaded = false;
        private bool loadedSavedServer;

        private ListView usersList;

        public string currentTopic { get; set; }

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

        public async void IrcPrompt(IrcServer server)
        {
            var dialog = new ContentDialog()
            {
                Title = "Join " + server.hostname,
                RequestedTheme = ElementTheme.Dark,
                //FullSizeDesired = true,
                MaxWidth = this.ActualWidth // Required for Mobile!
            };

            // Setup Content
            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = "To connect to this irc server, enter in a username first.",
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness
                {
                    Bottom = 8,
                },
            });

            var username = new TextBox
            {
                PlaceholderText = "Username",
                Text = "winircuser-" + (new Random()).Next(100, 1000)
            };

            panel.Children.Add(username);
            dialog.Content = panel;

            // Add Buttons
            dialog.PrimaryButtonText = "Join";
            dialog.PrimaryButtonClick += delegate
            {
                server.username = username.Text;

                var irc = new Net.IrcSocket();
                irc.server = server;
                MainPage.instance.Connect(irc);
            };

            dialog.SecondaryButtonText = "Cancel";
            dialog.ShowAsync();
        }

        public static MainPage instance;

        public MainPage()
        {
            this.InitializeComponent();
            this.LoadSettings();
            this.DataContext = this;

            connectedServers = new Dictionary<string, Net.Irc>();
            connectedServersList = new ObservableCollection<string>();

            currentChannel = "";
            currentServer = "";
            currentTopic = "";

            Loaded += MainPage_Loaded;

            var inputPane = InputPane.GetForCurrentView();
            inputPane.Showing += this.InputPaneShowing;
            inputPane.Hiding += this.InputPaneHiding;

            Window.Current.SizeChanged += Current_SizeChanged;

            SidebarFrame.Navigated += SidebarFrame_Navigated;

            this.ListBoxItemStyle = Application.Current.Resources["ListBoxItemStyle"] as Style;
            this.CommandHandler = new CommandHandler();

            var uiMode = UIViewSettings.GetForCurrentView().UserInteractionMode;

            if (uiMode == Windows.UI.ViewManagement.UserInteractionMode.Touch)
            {
                TabButton.Width = 48;
            }
            else
            {
                TabButton.Width = 0;
            }

            instance = this;
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

            ManageTitleBar();
            SettingsLoaded = true;

        }

        internal void UpdateUi()
        {
            if (Config.Contains(Config.FontFamily))
            {
                this.messagesView.FontFamily = new FontFamily(Config.GetString(Config.FontFamily));
            }

            if (Config.Contains(Config.FontSize))
            {
                this.messagesView.FontSize = Convert.ToDouble(Config.GetString(Config.FontSize));
            }

            if (Config.Contains(Config.ReducedPadding))
            {
                Thickness padding = new Thickness();
                padding.Left = 10;
                padding.Right = 10;
                if (Config.GetBoolean(Config.ReducedPadding))
                {
                    padding.Top = 4;
                    padding.Bottom = 4;
                }
                else
                {
                    padding.Top = 10;
                    padding.Bottom = 10;
                }

                var res = new ResourceDictionary { Source = new Uri("ms-appx:///Styles/Styles.xaml", UriKind.Absolute) };

                var style = res["ListBoxItemStyle"] as Style;

                foreach (var item in style.Setters.Cast<Setter>().Where(item => item.Property == PaddingProperty))
                    style.Setters.Remove(item);

                style.Setters.Add(new Setter(PaddingProperty, padding));

                this.ListBoxItemStyle = style;
                this.channelList.ItemContainerStyle = style;
            }

            if (Config.Contains(Config.HideStatusBar))
            {
                var isStatusBarPresent = ApiInformation.IsTypePresent(typeof(StatusBar).ToString());
                if (isStatusBarPresent)
                {
                    StatusBar statusBar = StatusBar.GetForCurrentView();
                    if (Config.GetBoolean(Config.HideStatusBar))
                        statusBar.HideAsync();
                    else
                        statusBar.ShowAsync();
                }

            }

        }

        public TextBox GetInputBox()
        {
            return ircMsgBox;
        }

        public ListBox GetChannelList()
        {
            return channelList;
        }


        private void ManageTitleBar()
        {
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            var darkTheme = Config.GetBoolean(Config.DarkTheme);

            var background = darkTheme ? ParseColor("#FF1F1F1F") : ParseColor("#FFE6E6E6");
            var backgroundInactive = darkTheme ? ParseColor("#FF2B2B2B") : ParseColor("#FFF2F2F2");
            var foreground = darkTheme ? ParseColor("#FFFFFFFF") : ParseColor("#FF000000");

            titleBar.BackgroundColor = background;
            titleBar.InactiveBackgroundColor = backgroundInactive;
            titleBar.ButtonHoverBackgroundColor = backgroundInactive;
            titleBar.ButtonBackgroundColor = background;
            titleBar.ButtonInactiveBackgroundColor = backgroundInactive;
            titleBar.ButtonForegroundColor = foreground;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            serversCombo.ItemsSource = connectedServersList;

            try
            {
                serversOSH = new ObjectStorageHelper<ObservableCollection<String>>(StorageType.Roaming);
                servers = await serversOSH.LoadAsync();

                serversListOSH = new ObjectStorageHelper<List<Net.IrcServer>>(StorageType.Roaming);
                serversList = await serversListOSH.LoadAsync();
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("Error when loading saved servers: " + ex.Message);
                await dialog.ShowAsync();
            }

            UpdateUi();

            serversSavedCombo.ItemsSource = servers;
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            serverConnect.HorizontalOffset = (Window.Current.Bounds.Width - connectDialogRoot.ActualWidth) / 2;
            serverConnect.VerticalOffset = (Window.Current.Bounds.Height - connectDialogRoot.ActualHeight) / 2;
        }

        private void InputPaneHiding(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            this.mainGrid.Margin = new Thickness();
        }

        private void InputPaneShowing(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            if (ircMsgBox.FocusState != FocusState.Unfocused)
            {
                this.mainGrid.Margin = new Thickness(0, -48, 0, args.OccludedRect.Height);
                args.EnsuredFocusedElementInView = true;
            }
            ScrollToBottom();
        }

        private async void ScrollToBottom()
        {
            if (messagesScroll != null)
            {
                await Task.Delay(1); // wait a millisecond to render first
                messagesScroll.ChangeView(null, messagesScroll.ScrollableHeight, null, false);
            }
        }

        private void ircMsgBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (currentChannel == null || currentServer == null || currentServer == "" || currentChannel == "")
            {
                return;
            }

            if ((e.Key == Windows.System.VirtualKey.Enter) && (ircMsgBox.Text != ""))
            {
                CommandHandler.HandleCommand(connectedServers[currentServer], ircMsgBox.Text);

                ircMsgBox.Text = "";
            }
            else if ((e.Key == Windows.System.VirtualKey.Tab) && (ircMsgBox.Text != ""))
            {
                e.Handled = true;

                TabComplete();
            }
        }

        private void TabComplete()
        {
            if (currentChannel == null || currentServer == null || currentServer == "" || currentChannel == "") 
            {
                return;
            }

            var users = connectedServers[currentServer].GetRawUsers(currentChannel);
            var words = ircMsgBox.Text.Split(' ');
            var word = words[words.Length - 1];
            var isFirst = (words.Length == 1);

            if (word.Length == 0)
                return;

            foreach (var user in users)
            {
                if (user.StartsWith(word))
                {
                    if (isFirst)
                    {
                        ircMsgBox.Text = user + ": ";
                    }
                    else
                    {
                        words[words.Length - 1] = words[words.Length - 1].Replace(word, user);
                        ircMsgBox.Text = String.Join(" ", words) + " ";
                    }
                    ircMsgBox.SelectionStart = ircMsgBox.Text.Length;
                    ircMsgBox.SelectionLength = 0;
                    ircMsgBox.Focus(FocusState.Keyboard);
                    break;
                }
            }
        }

        private void TabButton_Clicked(object sender, RoutedEventArgs e)
        {
            TabComplete();
        }

        private void ChannelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (channelList.SelectedItem == null)
                    return;

                var channel = channelList.SelectedItem.ToString();
                SwitchChannel(currentServer, channel);
                UpdateUsers();
            }
            catch (Exception ex)
            {
                var toast = Irc.CreateBasicToast(ex.Message, ex.StackTrace);
                toast.ExpirationTime = DateTime.Now.AddDays(2);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
        }

        public void SwitchChannel(string server, string channel)
        {
            if (currentServer != server)
            {
                serversCombo.SelectedItem = server;
            }

            connectedServers[currentServer].SwitchChannel(channel);
            currentChannel = channel;
            messagesView.ItemsSource = connectedServers[currentServer].channelBuffers[channel];
            connectedServers[currentServer].channelBuffers[channel].CollectionChanged += (s, args) => ScrollToBottom();
            ScrollToBottom();

            if (SplitView.DisplayMode == SplitViewDisplayMode.Overlay)
                SplitView.IsPaneOpen = false;

            channelList.SelectedValue = channel;
            if (connectedServers[currentServer].GetChannelTopic(channel) != null)
                TopicText.Text = connectedServers[currentServer].GetChannelTopic(channel);
        }

        public Irc GetCurrentServer()
        {
            return connectedServers[currentServer];
        }

        public void MentionReply(string ircserver, string channel, string message)
        {
            connectedServers[ircserver].MentionReply(channel, message);
        }

        private void ToggleSplitView(object sender, RoutedEventArgs e)
        {
            SplitView.IsPaneOpen = !SplitView.IsPaneOpen;
        }

        private void ShowConnectPopup(object sender, RoutedEventArgs e)
        {
            serverConnect.IsOpen = !serverConnect.IsOpen;
        }

        private void ConnectDialog_Loaded(object sender, RoutedEventArgs e)
        {
            serverConnect.HorizontalOffset = (Window.Current.Bounds.Width - connectDialogRoot.ActualWidth) / 2;
            serverConnect.VerticalOffset = (Window.Current.Bounds.Height - connectDialogRoot.ActualHeight) / 2;
        }

        private void CloseDialogClick(object sender, RoutedEventArgs e)
        {
            serverConnect.IsOpen = !serverConnect.IsOpen;
        }

        private void ConnectButtonClick(object sender, RoutedEventArgs e)
        {
            if (connectedServersList.Contains(server.Text) || connectedServersList.Contains(hostname.Text))
            {
                serverConnect.IsOpen = !serverConnect.IsOpen;
                return;
            }

            // create the irc object 
            var ircServer = CreateIrcServer();

            if (ircServer.invalid)
            {
                return;
            }

            Net.Irc irc;
            if (webSocket.IsOn)
                irc = new Net.IrcWebSocket();
            else
                irc = new Net.IrcSocket();

            irc.server = ircServer;

            Connect(irc);

            // close the dialog
            serverConnect.IsOpen = !serverConnect.IsOpen;
        }

        public void Connect(Irc irc)
        {
            if (connectedServersList.Contains(irc.server.name)) return;
            if (connectedServersList.Contains(irc.server.hostname)) return;

            irc.HandleDisconnect += HandleDisconnect;

            // connect
            irc.Connect();

            // link the server up to the lists
            connectedServers.Add(irc.server.name, irc);
            connectedServersList.Add(irc.server.name);
            serversCombo.SelectedItem = irc.server.name;
            currentServer = irc.server.name;
            channelList.ItemsSource = connectedServers[currentServer].channelList;
        }

        private void serversList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serversCombo.SelectedItem == null)
                return;

            currentServer = serversCombo.SelectedItem.ToString();
            channelList.ItemsSource = connectedServers[currentServer].channelList;
            if (connectedServers[currentServer].channelList.Contains("Server"))
                SwitchChannel(currentServer, "Server");
            UpdateUsers(true);
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            var ircServer = CreateIrcServer();

            if (ircServer.invalid)
            {
                return;
            }

            if (servers == null)
            {
                servers = new ObservableCollection<string>();
                serversList = new List<IrcServer>();
                serversSavedCombo.ItemsSource = servers;
            }

            foreach (var serverCheck in serversList)
            {
                if (serverCheck.name == server.Text)
                {
                    var name = serverCheck.name;
                    serversList.Remove(serverCheck);
                    servers.Remove(name);

                    serversListOSH.SaveAsync(serversList);
                    serversOSH.SaveAsync(servers);
                    break;
                }
            }

            servers.Add(ircServer.name);
            serversList.Add(ircServer);

            serversSavedCombo.SelectedItem = ircServer.name;

            serversListOSH.SaveAsync(serversList);
            serversOSH.SaveAsync(servers);
        }

        internal IrcServer CreateIrcServer()
        {
            Net.IrcServer ircServer = new Net.IrcServer();

            if (hostname.Text == "")
            {
                return ShowFormError("No hostname entered!");
            }

            if (Uri.CheckHostName(hostname.Text) == UriHostNameType.Unknown)
            {
                return ShowFormError("Hostname is incorrect!");
            }

            int portInt;

            if (port.Text == "")
            {
                return ShowFormError("No port entered!");
            }

            if (!int.TryParse(port.Text, out portInt))
            {
                return ShowFormError("Port is not a number!");
            }

            if (username.Text == "")
            {
                return ShowFormError("No username entered!");
            }

            if (username.Text.Contains(" "))
            {
                return ShowFormError("Usernames cannot contain spaces!");
            }

            ircServer.hostname = hostname.Text;
            ircServer.port = portInt;
            ircServer.ssl = ssl.IsOn;
            ircServer.username = username.Text;
            ircServer.password = password.Password;
            ircServer.name = server.Text;
            ircServer.webSocket = webSocket.IsOn;
            ircServer.channels = channels.Text;
            ircServer.invalid = false;

            formError.Height = 0;

            if (ircServer.name == "") ircServer.name = ircServer.hostname;

            return ircServer;
        }

        // kek
        private IrcServer ShowFormError(string error)
        {
            formError.Height = 19;
            formError.Text = error;
            return new IrcServer
            {
                invalid = true
            };
        }

        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (serversSavedCombo.SelectedItem == null) return; 
            foreach (var ircServer in serversList)
            {
                if (ircServer.name == serversSavedCombo.SelectedItem.ToString())
                {
                    var name = ircServer.name;
                    serversList.Remove(ircServer);
                    servers.Remove(name);

                    serversListOSH.SaveAsync(serversList);
                    serversOSH.SaveAsync(servers);
                    break;
                }
            }
        }

        private async void savedServersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            loadedSavedServer = true;
            if (serversSavedCombo.SelectedItem == null)
            {
                return;
            }

            if (serversList == null)
            {
                var dialog = new MessageDialog("Your saved servers have been corrupted for some reason. Clearing them");
                await dialog.ShowAsync();

                servers = new ObservableCollection<string>();
                serversList = new List<IrcServer>();
                serversSavedCombo.ItemsSource = servers;
                return;
            }

            if (!serversList.Any(server => server.name == serversSavedCombo.SelectedItem.ToString())) return;
            var ircServer = serversList.First(server => server.name == serversSavedCombo.SelectedItem.ToString());

            hostname.Text = ircServer.hostname;

            port.Text = ircServer.port.ToString();
            ssl.IsOn = ircServer.ssl;
            username.Text = ircServer.username;
            password.Password = ircServer.password;
            server.Text = ircServer.name;
            webSocket.IsOn = ircServer.webSocket;
            if (ircServer.channels != null)
                channels.Text = ircServer.channels;               

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

        public async void HandleDisconnect(Irc irc)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (connectedServersList.Count > 1)
                {
                    serversCombo.SelectedItem = connectedServersList[0];
                    currentServer = connectedServersList[0];
                    channelList.ItemsSource = connectedServers.Values.First().channelList;
                }

                connectedServers.Remove(irc.server.name);
                connectedServersList.Remove(irc.server.name);
                channelList.ItemsSource = null;
                messagesView.ItemsSource = null;
            });
        }

        private async void webSocket_Toggled(object sender, RoutedEventArgs e)
        {
            if (loadedSavedServer)
                return;

            if (webSocket.IsOn)
            {
                // Create the message dialog and set its content
                var messageDialog = new MessageDialog("To use this feature, you'll need to setup an IRCForwarder on a server to forward an irc server to a websocket.");

                // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
                messageDialog.Commands.Add(new UICommand(
                    "Show Me How",
                    new UICommandInvokedHandler(this.CommandInvokedHandler)));
                messageDialog.Commands.Add(new UICommand(
                    "Close",
                    new UICommandInvokedHandler(this.CommandInvokedHandler)));

                // Set the command that will be invoked by default
                messageDialog.DefaultCommandIndex = 0;

                // Set the command to be invoked when escape is pressed
                messageDialog.CancelCommandIndex = 1;

                // Show the message dialog
                await messageDialog.ShowAsync();
            }
        }

        private async void CommandInvokedHandler(IUICommand command)
        {
            if (command.Label == "Show Me How")
            {
                var uri = new Uri("https://rymate.co.uk/using-ircforwarder");
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SidebarFrame.BackStack.Clear();
            SidebarHeader.ShowBackButton = false;

            if (!(SidebarFrame.Content is SettingsView))
            {
                SidebarFrame.Navigate(typeof(SettingsView));
                var settingsView = (SettingsView)SidebarFrame.Content;

                if (settingsView != null)
                    settingsView.Header = SidebarHeader;
            }
            SidebarHeader.Title = "Settings";
            ToggleSidebar();
        }

        private void PeopleButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentChannel == "" || currentServer == "" || !connectedServers.ContainsKey(currentServer))
            {
                return;
            }
            SidebarFrame.BackStack.Clear();
            SidebarHeader.ShowBackButton = false;

            UpdateUsers();
            SidebarHeader.Title = "Channel Users";
            ToggleSidebar();
        }

        private void UpdateUsers(bool clear = false)
        {
            if (currentChannel == "" || currentServer == "" || !connectedServers.ContainsKey(currentServer))
            {
                return;
            }

            var users = new ObservableCollection<string>();

            if (!clear)
                users = connectedServers[currentServer].GetChannelUsers(currentChannel);

            if (!(SidebarFrame.Content is UsersView))
            {
                SidebarFrame.Navigate(typeof(UsersView));
            }

            var usersView = (UsersView)SidebarFrame.Content;

            if (usersView != null)
                usersView.UpdateUsers(users);

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
        }

        private Boolean SidebarPinned()
        {
            return SidebarSplitView.DisplayMode == SplitViewDisplayMode.Inline;
        }

        private void ToggleSidebar()
        {
            if (!SidebarPinned() || (SidebarPinned() && !SidebarSplitView.IsPaneOpen))
            {
                SidebarSplitView.IsPaneOpen = !SidebarSplitView.IsPaneOpen;
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

    }

}
