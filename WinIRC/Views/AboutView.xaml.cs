using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Net;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutView : Page
    {
        public AboutView()
        {
            this.InitializeComponent();

            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            var appVersion = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);

            AppNameBlock.Text = "WinIRC v" + appVersion;
        }

        private async void VisitWebsite(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://rymate.co.uk");
            await Windows.System.Launcher.LaunchUriAsync(uri);

        }

        private async void RateApp(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://www.microsoft.com/store/apps/9nblggh2p0rf");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void JoinSupport(object sender, RoutedEventArgs e)
        {
            WinIrcServer server = new WinIrcServer
            {
                Name = "WinIRC Support (Freenode)",
                Hostname = "irc.libera.chat",
                Port = 6697,
                Ssl = true,
                Channels = "#WinIRC"
            };

            MainPage.instance.IrcPrompt(server);
        }

        private async void VisitGithub(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://github.com/rymate1234/WinIRC/issues/");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}
