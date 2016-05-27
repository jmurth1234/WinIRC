using Microsoft.QueryStringDotNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
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
using WinIRC.Net;
using WinRtUtility;

namespace WinIRC
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);

            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        private void SetTheme()
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
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                this.SetTheme();

                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                if (!e.PrelaunchActivated)
                {
                    //TODO: maybe add some stuff here if needed.
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }


            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }

        protected override void OnActivated(IActivatedEventArgs e)
        {
            // Get the root frame
            Frame rootFrame = Window.Current.Content as Frame;

            var loaded = true;
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                this.SetTheme();

                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
                loaded = false;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage));

                // Ensure the current window is active
                Window.Current.Activate();
                loaded = false;
            }

            // Handle toast activation
            if (e.Kind == ActivationKind.ToastNotification && loaded)
            {
                var args = e as ToastNotificationActivatedEventArgs;
                var toastActivationArgs = args;

                // Parse the query string
                QueryString qryStr = QueryString.Parse(toastActivationArgs.Argument);

                // See what action is being requested 
                if (qryStr["action"] == "reply")
                {
                    string channel = qryStr["channel"];
                    string server = qryStr["server"];
                    string username = qryStr["username"];

                    var message = args.UserInput["tbReply"];

                    var mainPage = (MainPage)rootFrame.Content;

                    if (!mainPage.connectedServersList.Contains(server))
                    {
                        return;
                    }

                    if (mainPage != null)
                        mainPage.MentionReply(server, channel, username + ": " + message);

                    if (!mainPage.currentChannel.Equals(channel))
                        mainPage.SwitchChannel(server, channel);
                }
                else if (qryStr["action"] == "viewConversation")
                { 
                    // The conversation ID retrieved from the toast args
                    string channel = qryStr["channel"];
                    string server = qryStr["server"];

                    var mainPage = (MainPage)rootFrame.Content;

                    if (mainPage == null) return;

                    if (!mainPage.connectedServersList.Contains(server))
                    {
                        return;
                    }

                    // If we're already viewing that channel, do nothing
                    if (!mainPage.currentChannel.Equals(channel))
                        mainPage.SwitchChannel(server, channel);

                }

                // If we're loading the app for the first time, place the main page on
                // the back stack so that user can go back after they've been
                // navigated to the specific page
                if (rootFrame.BackStack.Count == 0)
                    rootFrame.BackStack.Add(new PageStackEntry(typeof(MainPage), null, null));
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

                IrcServer server = new IrcServer
                {
                    name = uri.Host,
                    hostname = uri.Host,
                    port = port,
                    ssl = uri.Scheme == "ircs",
                };

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
