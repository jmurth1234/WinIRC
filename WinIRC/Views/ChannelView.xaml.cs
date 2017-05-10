using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Handlers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChannelView : Page
    {
        private IrcUiHandler IrcHandler = IrcUiHandler.Instance;

        public string currentChannel { get; set; }
        public string currentServer { get; set; }

        public ChannelView()
        {
            this.InitializeComponent();

            var uiMode = UIViewSettings.GetForCurrentView().UserInteractionMode;

            if (uiMode == Windows.UI.ViewManagement.UserInteractionMode.Touch)
            {
                TabButton.Width = 48;
            }
            else
            {
                TabButton.Width = 0;
            }

            UpdateUi();
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var args = ((string[])e.Parameter);

            SetChannel(args[0], args[1]);
        }


        internal void SetChannel(string server, string channel)
        {
            currentServer = server;
            currentChannel = channel;

            messagesView.ItemsSource = null;

            messagesView.ItemsSource = IrcHandler.connectedServers[currentServer].channelBuffers[currentChannel];

            IrcHandler.connectedServers[currentServer].channelBuffers[currentChannel].CollectionChanged += (s, args) => {
                ScrollToBottom(currentServer, currentChannel);
            };

            ScrollToBottom(currentServer, currentChannel);
        }


        public TextBox GetInputBox()
        {
            return ircMsgBox;
        }

        internal async void ScrollToBottom(string server, string channel)
        {
            if (channel != currentChannel || server != currentServer)
            {
                return;
            }

            if (messagesScroll != null)
            {
                await Task.Delay(1); // wait a millisecond to render first
                messagesScroll.ChangeView(null, messagesScroll.ScrollableHeight, null, false);
            }
        }

        public void ircMsgBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (currentChannel == null || currentServer == null || currentServer == "" || currentChannel == "")
            {
                return;
            }

            IrcHandler.IrcTextBoxHandler(ircMsgBox, e, currentServer, currentChannel);
        }


        private void TabButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (currentChannel == null || currentServer == null || currentServer == "" || currentChannel == "")
            {
                return;
            }

            IrcHandler.TabComplete(ircMsgBox, currentServer, currentChannel);
        }


        public void UpdateUi()
        {
            if (Config.Contains(Config.FontFamily))
            {
                this.messagesView.FontFamily = new FontFamily(Config.GetString(Config.FontFamily));
            }

            if (Config.Contains(Config.FontSize))
            {
                this.messagesView.FontSize = Convert.ToDouble(Config.GetString(Config.FontSize));
            }
        }
    }
}
