using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Template10.Services.NavigationService;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Net;
using WinIRC.Utils;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FirstRunPage : Page
    {
        public ObservableCollection<WinIrcServer> Servers = new ObservableCollection<WinIrcServer>();
        private string RegexPattern = "((?:[A-z]+[a-z0-9_|-]*))+";
        public FirstRunPage()
        {
            this.InitializeComponent();

            // totally a great way of doing this
            Servers.Add(new WinIrcServer()
            {
                Name = "Esper",
                Hostname = "irc.esper.net",
                Port = 6697,
                Ssl = true,
            });

            Servers.Add(new WinIrcServer()
            {
                Name = "Freenode",
                Hostname = "chat.freenode.net",
                Port = 6697,
                Ssl = true,
            });

            Servers.Add(new WinIrcServer()
            {
                Name = "IRCNet",
                Hostname = "irc.atw-inter.net",
                Port = 6667,
                Ssl = false,
            });

            Servers.Add(new WinIrcServer()
            {
                Name = "QuakeNet",
                Hostname = "irc.quakenet.org",
                Port = 6667,
                Ssl = false,
            });

            Servers.Add(new WinIrcServer()
            {
                Name = "EFNet",
                Hostname = "efnet.Port80.se",
                Port = 6697,
                Ssl = true,
            });

            Servers.Add(new WinIrcServer()
            {
                Name = "Rizon",
                Hostname = "irc.rizon.net",
                Port = 6697,
                Ssl = true,
            });

            Servers.Add(new WinIrcServer()
            {
                Name = "Undernet",
                Hostname = "ix1.undernet.org",
                Port = 6667,
                Ssl = false,
            });

            Loaded += FirstRunPage_Loaded;

        }

        private void FirstRunPage_Loaded(object sender, RoutedEventArgs e)
        {
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            Window.Current.SetTitleBar(TitleRect);

            Username.Text = "winircuser-" + (new Random()).Next(100, 1000);
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IrcServers store = IrcServers.Instance;
            if (Regex.Matches(Username.Text, RegexPattern).Count != 1)
            {
                Error.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                Config.SetString(Config.DefaultUsername, Username.Text);
            }

            foreach (WinIrcServer server in ServListBox.SelectedItems)
            {
                server.Username = Username.Text;
                await store.AddServer(server);
            }

            Config.SetBoolean(Config.FirstRun, true);
            await App.Current.NavigationService.NavigateAsync(typeof(MainPage));
            App.Current.NavigationService.ClearHistory();
        }

    }
}
