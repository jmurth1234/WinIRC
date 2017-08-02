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
        public ObservableCollection<IrcServer> Servers = new ObservableCollection<IrcServer>();
        private string RegexPattern = "((?:[A-z]+[a-z0-9_|-]*))+";
        public FirstRunPage()
        {
            this.InitializeComponent();

            // totally a great way of doing this
            Servers.Add(new IrcServer()
            {
                name = "Esper",
                hostname = "irc.esper.net",
                port = 6697,
                ssl = true,
            });

            Servers.Add(new IrcServer()
            {
                name = "Freenode",
                hostname = "chat.freenode.net",
                port = 6697,
                ssl = true,
            });

            Servers.Add(new IrcServer()
            {
                name = "IRCNet",
                hostname = "irc.atw-inter.net",
                port = 6667,
                ssl = false,
            });

            Servers.Add(new IrcServer()
            {
                name = "QuakeNet",
                hostname = "irc.quakenet.org",
                port = 6667,
                ssl = false,
            });

            Servers.Add(new IrcServer()
            {
                name = "EFNet",
                hostname = "efnet.port80.se",
                port = 6697,
                ssl = true,
            });

            Servers.Add(new IrcServer()
            {
                name = "Rizon",
                hostname = "irc.rizon.net",
                port = 6697,
                ssl = true,
            });

            Servers.Add(new IrcServer()
            {
                name = "Undernet",
                hostname = "ix1.undernet.org",
                port = 6667,
                ssl = false,
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

            foreach (IrcServer server in ServListBox.SelectedItems)
            {
                server.username = Username.Text;
                await store.AddServer(server);
            }

            Config.SetBoolean(Config.FirstRun, true);
            await App.Current.NavigationService.NavigateAsync(typeof(MainPage));
            App.Current.NavigationService.ClearHistory();
        }

    }
}
