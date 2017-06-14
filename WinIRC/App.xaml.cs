using Microsoft.QueryStringDotNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Tweetinvi;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Handlers;
using WinIRC.Net;
using Tweetinvi.Models;
using Windows.Data.Xml.Dom;
using Windows.UI.Core;
using WinIRC.Utils;
using System.Threading.Tasks;
using Template10.Common;

namespace WinIRC
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    [Bindable]
    sealed partial class App : BootStrapper
    {
        public bool AppLaunched
        {
            get;
            private set;
        }

        public ITwitterCredentials TwitterCredentials
        {
            get;
            private set;
        }

        private int _NumberPings;
        public int NumberPings
        {
            get
            {
                return _NumberPings;
            }

            set
            {
                if (!IncrementPings)
                {
                    return;
                }

                _NumberPings = value;
                if (NumberPings > 0)
                {
                    setBadgeNumber(_NumberPings);
                }
                else
                {
                    clearBadge();
                }
            }
        }

        public bool IncrementPings = true;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(Microsoft.ApplicationInsights.WindowsCollectors.Metadata | Microsoft.ApplicationInsights.WindowsCollectors.Session);

            SetTheme();

            this.InitializeComponent();
            foreach (var current in BackgroundTaskRegistration.AllTasks)
            {
                current.Value.Unregister(true);
            }

            this.Suspending += OnSuspending;
            this.Resuming += App_Resuming;
        }

        private Boolean CanBackground = false;

        public void SetTheme()
        {
            try
            {
                if (Config.Contains(Config.DarkTheme))
                {
                    this.RequestedTheme = Config.GetBoolean(Config.DarkTheme) ? ApplicationTheme.Dark : ApplicationTheme.Light;
                }
                else
                {
                    Config.SetBoolean(Config.DarkTheme, true);
                    this.RequestedTheme = ApplicationTheme.Dark;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }

            if (AppLaunched)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                if (Config.Contains(Config.DarkTheme))
                {
                    MainPage.instance.RequestedTheme = Config.GetBoolean(Config.DarkTheme) ? ElementTheme.Dark : ElementTheme.Light;
                }
                else
                {
                    Config.SetBoolean(Config.DarkTheme, true);
                    rootFrame.RequestedTheme = ElementTheme.Dark;
                }

                MainPage.instance.ManageTitleBar();
            }
        }


        private void setBadgeNumber(int num)
        {
            // Get the blank badge XML payload for a badge number
            XmlDocument badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
            // Set the value of the badge in the XML to our number
            XmlElement badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
            badgeElement.SetAttribute("value", num.ToString());
            // Create the badge notification
            BadgeNotification badge = new BadgeNotification(badgeXml);
            // Create the badge updater for the application
            BadgeUpdater badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            // And update the badge
            badgeUpdater.Update(badge);
        }

        private void clearBadge()
        {
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name = "e">Details about the launch request and process.</param>
        public override async Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            try
            {
                var check = await BackgroundExecutionManager.RequestAccessAsync();
                CanBackground = check == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity || check == BackgroundAccessStatus.AllowedSubjectToSystemPolicy || check == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity || check == BackgroundAccessStatus.AlwaysAllowed;
            }
            catch
            {
                CanBackground = false;
            }

            if (startKind.Equals(StartKind.Activate))
                await Activated(args);
            else
                await InitApp(args);

            // Ensure the current window is active  
            Window.Current.Activate();
            Window.Current.Activated += Current_Activated;

            AppLaunched = true;
        }

        private void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                Debug.WriteLine("Deactivated " + DateTime.Now);
                IncrementPings = true;
            }
            else
            {
                Debug.WriteLine("Activated " + DateTime.Now);
                NumberPings = 0;
                IncrementPings = false;
            }
        }

        private async Task<bool> InitApp(IActivatedEventArgs e)
        {
            var loaded = true;
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!AppLaunched)
            {
                loaded = false;
                NumberPings = 0;
                var applicationView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                applicationView.SetPreferredMinSize(new Windows.Foundation.Size{Width = 360, Height = 240});
            }

            IrcServers servers = IrcServers.Instance;
            await servers.loadServersAsync();
            await servers.UpdateJumpList();

            if (!(NavigationService.Content is MainPage))
            {
                loaded = false;
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (e is LaunchActivatedEventArgs)
                {
                    var args = (e as LaunchActivatedEventArgs).Arguments;
                    await NavigationService.NavigateAsync(typeof(MainPage), args);
                } else
                {
                    await NavigationService.NavigateAsync(typeof(MainPage));
                }
            }
            else
            {
                var page = NavigationService.Content as MainPage;
                if (e is LaunchActivatedEventArgs)
                    page?.ConnectViaName((e as LaunchActivatedEventArgs).Arguments);
            }

            return loaded;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name = "sender">The Frame which failed navigation</param>
        /// <param name = "e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name = "sender">The source of the suspend request.</param>
        /// <param name = "e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //MainPage.instance.ExtendExecution();
            deferral.Complete();
        }

        private void App_Resuming(object sender, object e)
        {
            MainPage.instance.ExtendExecution();
        }

        private void ShowToast(string title, string message)
        {
            var toast = Irc.CreateBasicToast(title, message);
            var toastNotifier = ToastNotificationManager.CreateToastNotifier();
            toastNotifier.Show(toast);
        }

        protected async Task Activated(IActivatedEventArgs e)
        {
            // Initialise the app if it's not already open
            Debug.WriteLine("App activated!");
            var loaded = await InitApp(e);
            // Handle toast activation
            if (e.Kind == ActivationKind.ToastNotification && loaded && NavigationService.Content is MainPage)
            {
                MainPage mainPage = NavigationService.Content as MainPage;

                var args = e as ToastNotificationActivatedEventArgs;
                var toastActivationArgs = args;
                // Parse the query string
                QueryString qryStr = QueryString.Parse(toastActivationArgs.Argument);
                if (!qryStr.Contains("action"))
                    return;
                var ircHandler = IrcUiHandler.Instance;
                if (ircHandler == null)
                    return;
                // See what action is being requested 
                if (qryStr["action"] == "reply")
                {
                    string channel = qryStr["channel"];
                    string server = qryStr["server"];
                    string username = qryStr["username"];
                    var message = args.UserInput["tbReply"];
                    if (!ircHandler.connectedServersList.Contains(server))
                    {
                        return;
                    }

                    if (mainPage != null)
                        mainPage.MentionReply(server, channel, username + ": " + message);
                    if (!mainPage.currentChannel.Equals(channel))
                        mainPage.SwitchChannel(server, channel, false);
                }
                else if (qryStr["action"] == "viewConversation")
                {
                    // The conversation ID retrieved from the toast args
                    string channel = qryStr["channel"];
                    string server = qryStr["server"];

                    if (mainPage == null)
                        return;
                    if (!ircHandler.connectedServersList.Contains(server))
                    {
                        return;
                    }

                    // If we're already viewing that channel, do nothing
                    if (!mainPage.currentChannel.Equals(channel))
                        mainPage.SwitchChannel(server, channel, false);
                }
            }

            if (e.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs eventArgs = e as ProtocolActivatedEventArgs;
                // TODO: Handle URI activation
                // The received URI is eventArgs.Uri.AbsoluteUri
                var uri = eventArgs.Uri;
                var port = 0;
                if (uri.Port == 0)
                {
                    port = 6667;
                }
                else
                {
                    port = uri.Port;
                }

                IrcServer server = new IrcServer{name = uri.Host, hostname = uri.Host, port = port, ssl = uri.Scheme == "ircs", };
                if (uri.Segments.Length >= 2)
                {
                    server.channels += "#" + uri.Segments[1];
                }

                MainPage.instance.IrcPrompt(server);
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }
    }
}