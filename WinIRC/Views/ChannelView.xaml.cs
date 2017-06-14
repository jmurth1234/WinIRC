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
using WinIRC.Net;

namespace WinIRC.Views
{
    public sealed partial class ChannelView : Page
    {
        private IrcUiHandler IrcHandler = IrcUiHandler.Instance;
        private ChannelStore store;
        private bool ChannelLoaded;

        public string currentChannel { get; set; }
        public string currentServer { get; set; }

        public ChannelView()
        {
            this.InitializeComponent();

            UpdateUi();
        }

        public ChannelView(string server, string channel)
        {
            this.InitializeComponent();

            Loaded += (s, e) =>
            {
                SetChannel(server, channel);
            };

            Unloaded += ChannelView_Unloaded;

            UpdateUi();
        }

        private void ChannelView_Unloaded(object sender, RoutedEventArgs e)
        {
            Unload();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var args = ((string[])e.Parameter);

            SetChannel(args[0], args[1]);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            var args = ((string[])e.Parameter);

            Unload();
        }

        private void Unload()
        {
            if (ChannelLoaded)
            {
                messagesView.ItemsSource = null;
                store.TopicSetEvent -= ChannelView_TopicSetEvent;
                store = null;
                topicText.Text = "";
            }
        }

        internal void SetChannel(string server, string channel)
        {
            if (currentChannel != null && currentServer != null)
            {
                IrcHandler.connectedServers[currentServer].channelBuffers[currentChannel].CollectionChanged -= ChannelView_CollectionChanged;
                store.TopicSetEvent -= ChannelView_TopicSetEvent;
            }
            var servers = IrcHandler.connectedServers;

            if (!servers.ContainsKey(server) || !servers[server].channelBuffers.ContainsKey(channel))
            {
                return;
            }

            currentServer = server;
            currentChannel = channel;

            messagesView.ItemsSource = null;

            messagesView.ItemsSource = IrcHandler.connectedServers[currentServer].channelBuffers[currentChannel];

            store = IrcHandler.connectedServers[currentServer].channelStore[currentChannel];
            store.TopicSetEvent += ChannelView_TopicSetEvent;

            topicText.Text = store.Topic;

            IrcHandler.connectedServers[currentServer].channelBuffers[currentChannel].CollectionChanged += ChannelView_CollectionChanged;

            ScrollToBottom(currentServer, currentChannel);
            ChannelLoaded = true;
        }

        private void ChannelView_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ScrollToBottom(currentServer, currentChannel);
        }

        private void ChannelView_TopicSetEvent(string topic)
        {
            topicText.Text = topic;
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
            var uiMode = UIViewSettings.GetForCurrentView().UserInteractionMode;

            if (uiMode == Windows.UI.ViewManagement.UserInteractionMode.Touch)
            {
                TabButton.Width = 48;
            }
            else
            {
                TabButton.Width = 0;
            }

            if (Config.Contains(Config.FontFamily))
            {
                this.messagesView.FontFamily = new FontFamily(Config.GetString(Config.FontFamily));
            }

            if (Config.Contains(Config.FontSize))
            {
                this.messagesView.FontSize = Convert.ToDouble(Config.GetString(Config.FontSize));
            }

            if (currentChannel != "Server")
            {
                topicScroll.Visibility = MainPage.instance.ShowTopic ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                topicScroll.Visibility = Visibility.Collapsed;
            }
        }
    }
}
