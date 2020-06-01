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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConnectDialog : ContentDialog
    {

        private IrcServers ircServers = IrcServers.Instance;

        public IrcUiHandler IrcHandler = IrcUiHandler.Instance;
        private bool loadedSavedServer;

        public ConnectDialog()
        {
            this.InitializeComponent();

            this.Loaded += ConnectView_LoadedAsync;
        }

        private async void ConnectView_LoadedAsync(object sender, RoutedEventArgs e)
        {
            await ircServers.loadServersAsync();
            serversSavedCombo.ItemsSource = ircServers.Servers;

            username.Text = Config.GetString(Config.DefaultUsername);
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (IrcHandler.connectedServersList.Contains(server.Text) || IrcHandler.connectedServersList.Contains(hostname.Text))
            {
                this.Hide();
                return;
            }

            // create the irc object 
            var ircServer = CreateIrcServer();

            if (ircServer.invalid)
            {
                return;
            }

            var irc = ircServers.CreateConnection(ircServer);

            this.Hide();
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

            ircServers.AddServer(ircServer);
            serversSavedCombo.ItemsSource = ircServers.Servers;
            serversSavedCombo.SelectedItem = ircServer.Name;
        }

        internal WinIrcServer CreateIrcServer()
        {
            WinIrcServer ircServer = new WinIrcServer();

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

            ircServer.Hostname = hostname.Text;
            ircServer.Port = portInt;
            ircServer.Ssl = ssl.IsOn;
            ircServer.Username = username.Text;
            ircServer.Password = password.Password;
            ircServer.NickservPassword = nickservPassword.Password;
            ircServer.Name = server.Text;
            ircServer.webSocket = webSocket.IsOn;
            ircServer.Channels = channels.Text;
            ircServer.invalid = false;

            formError.Height = 0;

            if (ircServer.Name == "") ircServer.Name = ircServer.Hostname;

            return ircServer;
        }

        private void CloseDialogClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        // kek
        private WinIrcServer ShowFormError(string error)
        {
            formError.Height = 19;
            formError.Text = error;
            return new WinIrcServer
            {
                invalid = true
            };
        }

        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (serversSavedCombo.SelectedItem == null) return;

            ircServers.DeleteServer(serversSavedCombo.SelectedItem.ToString());
        }

        private async void savedServersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            loadedSavedServer = true;
            if (serversSavedCombo.SelectedItem == null)
            {
                return;
            }

            if (ircServers.Servers == null)
            {
                var dialog = new MessageDialog("Your saved servers have been corrupted for some reason. Clearing them");
                await dialog.ShowAsync();

                ircServers.Servers = new ObservableCollection<WinIrcServer>();
                serversSavedCombo.ItemsSource = ircServers;
                return;
            }

            var ircServer = ircServers.Get(serversSavedCombo.SelectedItem.ToString());

            hostname.Text = ircServer.Hostname;

            port.Text = ircServer.Port.ToString();
            ssl.IsOn = ircServer.Ssl;
            username.Text = ircServer.Username;
            password.Password = ircServer.Password;
            server.Text = ircServer.Name;
            webSocket.IsOn = ircServer.webSocket;

            if (ircServer.Channels != null)
                channels.Text = ircServer.Channels;

            if (ircServer.NickservPassword != null)
                nickservPassword.Password = ircServer.NickservPassword;

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
