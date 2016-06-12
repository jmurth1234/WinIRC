using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Handlers;
using WinIRC.Net;
using WinIRC.Utils;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConnectView : Page
    {
        private ObjectStorageHelper<ObservableCollection<string>> serversOSH;
        private ObjectStorageHelper<List<IrcServer>> serversListOSH;

        public ObservableCollection<String> servers { get; set; }
        public List<IrcServer> serversList { get; set; }

        public IrcUiHandler IrcHandler = IrcUiHandler.Instance;
        private bool loadedSavedServer;

        private Action CloseView { get; set; }

        public ConnectView()
        {
            this.InitializeComponent();

            Loaded += ConnectView_Loaded; 

            CloseView = MainPage.instance.CloseConnectView;
        }

        private async void ConnectView_Loaded(object sender, RoutedEventArgs e)
        {
            serversOSH = new ObjectStorageHelper<ObservableCollection<String>>(StorageType.Roaming);
            servers = await serversOSH.LoadAsync(Config.ServersStore);

            serversListOSH = new ObjectStorageHelper<List<Net.IrcServer>>(StorageType.Roaming);
            serversList = await serversListOSH.LoadAsync(Config.ServersListStore);

            serversSavedCombo.ItemsSource = servers;
        }

        private void ConnectButtonClick(object sender, RoutedEventArgs e)
        {
            if (IrcHandler.connectedServersList.Contains(server.Text) || IrcHandler.connectedServersList.Contains(hostname.Text))
            {
                CloseView();
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

            CloseView();
            MainPage.instance.Connect(irc);

            // close the dialog
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

                    serversListOSH.SaveAsync(serversList, Config.ServersListStore);
                    serversOSH.SaveAsync(servers, Config.ServersStore);
                    break;
                }
            }

            servers.Add(ircServer.name);
            serversList.Add(ircServer);

            serversSavedCombo.SelectedItem = ircServer.name;

            serversListOSH.SaveAsync(serversList, Config.ServersListStore);
            serversOSH.SaveAsync(servers, Config.ServersStore);
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

        private void CloseDialogClick(object sender, RoutedEventArgs e)
        {
            CloseView();
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

                    serversListOSH.SaveAsync(serversList, Config.ServersListStore);
                    serversOSH.SaveAsync(servers, Config.ServersStore);
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

        private async void CommandInvokedHandler(IUICommand command)
        {
            if (command.Label == "Show Me How")
            {
                var uri = new Uri("https://rymate.co.uk/using-ircforwarder");
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
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

    }
}
