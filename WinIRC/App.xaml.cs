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
using WinIRC.Views;
using Microsoft.ApplicationInsights;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.UI.Xaml.Markup;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;

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

        public bool SessionRevoked { get; private set; }

        public bool IncrementPings = true;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            AutoExtendExecutionSession = false;
            AppCenter.Start("ca57c9d3-f4bf-4bb0-82d0-64cd09de9ac6", typeof(Analytics), typeof(Crashes));

            SetTheme();

            this.InitializeComponent();
            foreach (var current in BackgroundTaskRegistration.AllTasks)
            {
                current.Value.Unregister(true);
            }
        }

        private Boolean CanBackground = false;
        private ExtendedExecutionSession session;

        public void SetTheme()
        {
            try
            {
                if (Config.Contains(Config.DarkTheme))
                {
                    var darkTheme = Config.GetBoolean(Config.DarkTheme);
                    this.RequestedTheme = darkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light;
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
                CanBackground = check == BackgroundAccessStatus.AllowedSubjectToSystemPolicy || check == BackgroundAccessStatus.AlwaysAllowed;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
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

            ExtendExecution();

            IrcServers servers = IrcServers.Instance;
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!AppLaunched)
            {
                await servers.loadServersAsync();
                await servers.UpdateJumpList();

                loaded = false;
                NumberPings = 0;
                var applicationView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                applicationView.SetPreferredMinSize(new Windows.Foundation.Size { Width = 360, Height = 240 });
            }

            if (!(NavigationService.Content is MainPage))
            {
                loaded = false;
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (Config.Contains(Config.FirstRun) || servers.Count != 0 || Config.Contains(Config.DefaultUsername))
                {
                    if (e is LaunchActivatedEventArgs)
                    {
                        var args = (e as LaunchActivatedEventArgs).Arguments;
                        await NavigationService.NavigateAsync(typeof(MainPage), args);
                    }
                    else
                    {
                        await NavigationService.NavigateAsync(typeof(MainPage));
                    }
                }
                else
                {
                    await NavigationService.NavigateAsync(typeof(FirstRunPage));
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

        /// <param name="e">Details about the suspend request.</param>
        public override async Task OnSuspendingAsync(object s, SuspendingEventArgs e, bool prelaunchActivated)
        {
            await base.OnSuspendingAsync(s, e, prelaunchActivated);

            if (CanBackground)
            {
                ExtendExecution();
                //var servers = IrcUiHandler.Instance.connectedServers.Values;

                //foreach (var server in servers)
                {
                    //if (server is IrcSocket)
                    {
                        // server.SocketTransfer();
                    }
                }
            }
        }

        public async void ExtendExecution()
        {
            Debug.WriteLine("Attempting extension");
            try
            {
                if (session != null)
                {
                    session.Revoked -= Session_Revoked;
                    session.Dispose();
                    session = null;
                }

                session = new ExtendedExecutionSession();
                session.Reason = ExtendedExecutionReason.Unspecified;
                session.Revoked += Session_Revoked;
                ExtendedExecutionResult result = await session.RequestExtensionAsync();

                switch (result)
                {
                    case ExtendedExecutionResult.Allowed:
                        break;

                    default:
                    case ExtendedExecutionResult.Denied:
                        SessionRevoked = true;
                        break;
                }
            }
            catch { }
        }

        private void Session_Revoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            if (args.Reason == ExtendedExecutionRevokedReason.SystemPolicy)
            {
                SessionRevoked = true;
            }
            else
            {
                ExtendExecution();
            }
        }

        public override void OnResuming(object s, object e, AppExecutionState previousExecutionState)
        {
            base.OnResuming(s, e, previousExecutionState);

            if (CanBackground)
            {
                var servers = IrcUiHandler.Instance.connectedServers.Values;

                foreach (var server in servers)
                {
                    if (server is IrcSocket)
                    {
                        // server.SocketReturn();
                    }
                }
            }

            if (SessionRevoked)
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MainPage.instance.ShowTeachingTip();
                });
                SessionRevoked = false;
            }
        }

        protected async override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var taskInstance = args.TaskInstance;
            var deferral = taskInstance.GetDeferral();
            Debug.WriteLine("Attempting background execution: " + taskInstance.Task.Name);
            try
            {
                var details = taskInstance.TriggerDetails as SocketActivityTriggerDetails;
                var socketInformation = details.SocketInformation;

                var servers = IrcUiHandler.Instance.connectedServers.Values;

                IrcSocket irc = null;

                foreach (var server in servers)
                {
                    Debug.WriteLine("Irc server name " + server.BackgroundTaskName + " - Task name " + taskInstance.Task.Name);

                    if (server is IrcSocket && taskInstance.Task.Name == server.BackgroundTaskName)
                    {
                        irc = server as IrcSocket;
                    }
                }

                if (irc == null)
                {
                    Debug.WriteLine("Unable to get irc server: " + taskInstance.Task.Name);
                    return;
                }

                Debug.WriteLine("Able to get irc server: " + taskInstance.Task.Name);

                switch (details.Reason)
                {
                    case SocketActivityTriggerReason.SocketActivity:
                    case SocketActivityTriggerReason.KeepAliveTimerExpired:
                        var socket = socketInformation.StreamSocket;
                        using (DataReader reader = new DataReader(socket.InputStream))
                        using (DataWriter writer = new DataWriter(socket.OutputStream))
                        {
                            reader.InputStreamOptions = InputStreamOptions.Partial;
                            await irc.ReadFromServer(reader, writer);
                        }
                        break;
                    case SocketActivityTriggerReason.SocketClosed:
                        // implement reconnecting
                        break;
                    default:
                        break;
                }
                deferral.Complete();
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
                Debug.WriteLine(exception.StackTrace);
                deferral.Complete();
            }
        }

        private void ShowToast(string title, string message)
        {
            var toast = IrcUWPBase.CreateBasicToast(title, message);
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

                WinIrcServer server = new WinIrcServer
                {
                    Name = uri.Host,
                    Hostname = uri.Host,
                    Port = port,
                    Ssl = uri.Scheme == "ircs"
                };
                if (uri.Segments.Length >= 2)
                {
                    server.Channels += "#" + uri.Segments[1];
                }

                MainPage.instance.IrcPrompt(server);
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }
    }
}
