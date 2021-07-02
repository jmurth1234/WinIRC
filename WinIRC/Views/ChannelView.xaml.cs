using IrcClientCore;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Handlers;
using WinIRC.Net;
using WinIRC.Ui;

namespace WinIRC.Views
{
    public sealed partial class ChannelView : Page, INotifyPropertyChanged
    {
        private IrcUiHandler IrcHandler = IrcUiHandler.Instance;
        private ChannelStore store;
        private bool ChannelLoaded;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _channel;
        private string _server;

        public string Channel
        {
            get => _channel;
            set
            {
                _channel = value;
                this.NotifyPropertyChanged();
            }
        }

        public string Server
        {
            get => _server;
            set
            {
                _server = value;
                this.NotifyPropertyChanged();
            }
        }

        private ObservableCollection<MessageGroup> currentBuffer;
        public ObservableCollection<MessageGroup> CurrentBuffer
        {
            get => currentBuffer; set
            {
                currentBuffer = value;
                this.NotifyPropertyChanged();
            }
        }

        private DataTemplate template;
        public DataTemplate CurrentTemplate
        {
            get => template; set
            {
                template = value;
                this.NotifyPropertyChanged();
            }
        }

        public AppWindow Window { get; }

        public ChannelView()
        {
            this.InitializeComponent();
            UpdateUi();
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ChannelView(string server, string channel) : this(server, channel, null) { 
        }

        public ChannelView(string server, string channel, AppWindow window)
        {
            this.InitializeComponent();

            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.ThemeShadow"))
            {
                Shadow.Receivers.Add(Scroll);

                TopArea.Translation += new Vector3(0, 0, 12);
            }

            Server = server;
            Channel = channel;

            Unloaded += ChannelView_Unloaded;

            var visibility = window != null ? Visibility.Visible : Visibility.Collapsed;
            this.titlebar.Visibility = visibility;
            this.titlebar.Text = $"{Channel} | {Server}";
            this.Window = window;

            if (window == null && CompactToggle != null)
            {
                CompactToggle.Visibility = Visibility.Collapsed;   
            }

            MainPage.instance.UiUpdated += Instance_UiUpdated;
            SetChannel(server, channel);
        }

        private void Instance_UiUpdated(object sender, EventArgs e)
        {
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
            Unload();
        }

        private void Unload()
        {
            if (ChannelLoaded)
            {
                CurrentBuffer = null;
                topicText.Text = "";
            }
        }

        private Channel GetChannel()
        {
            var servers = IrcHandler.connectedServers;

            if (servers.Count == 0 || !servers.ContainsKey(Server)) return null;

            Channel channelStore = null;

            if (servers[Server].ChannelList.Contains(Channel))
            {
                channelStore = IrcHandler.connectedServers[Server].ChannelList[Channel];
            }
            else if (Channel == "Server" || Channel == "")
            {
                channelStore = IrcHandler.connectedServers[Server].ChannelList.ServerLog;
            }

            return channelStore;
        }

        internal void SetChannel(string server, string channel)
        {
            if (Channel != null && Server != null && ChannelLoaded)
            {
                var chan = Channel == "Server"
                    ? IrcHandler.connectedServers[Server].ChannelList.ServerLog
                    : IrcHandler.connectedServers[Server].ChannelList[Channel];
            }

            var servers = IrcHandler.connectedServers;

            if (!servers.ContainsKey(server))
            {
                return;
            }
            Server = server;
            Channel = channel;

            var channelStore = GetChannel();
            if (channelStore == null) return;

            store = channelStore.Store;

            topicText.Text = store.Topic;

            UpdateUi();

            ChannelLoaded = true;
        }

        private void SetupBuffer()
        {
            var channelStore = GetChannel();
            if (channelStore == null) return;

            var grouped = new MessageGrouper((channelStore.Buffers as UWPBuffer).Collection);

            this.CurrentTemplate = this.Resources[Config.GetBoolean(Config.ModernChat) ? "ModernTemplate" : "ClassicTemplate"] as DataTemplate;
            this.CurrentBuffer = grouped.Grouped;
        }

        public TextBox GetInputBox()
        {
            return ircTextBox.Inner;
        }

        public void UpdateUi()
        {
            SetupBuffer();

            if (Config.Contains(Config.FontFamily))
            {
                this.messagesView.FontFamily = new FontFamily(Config.GetString(Config.FontFamily));
            }

            if (Config.Contains(Config.FontSize))
            {
                this.messagesView.FontSize = Convert.ToDouble(Config.GetInt(Config.FontSize, 14));
            }

            if (Channel != "Server")
            {
                topicScroll.Visibility = MainPage.instance.ShowTopic ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                topicScroll.Visibility = Visibility.Collapsed;
            }
            var darktheme = Config.GetBoolean(Config.DarkTheme);
            this.RequestedTheme = darktheme ? ElementTheme.Dark : ElementTheme.Light;

            var sidebarColor = Config.GetBoolean(Config.DarkTheme) ? ColorUtils.ParseColor("#FF222222") : ColorUtils.ParseColor("#FFDDDDDD");
            Brush brush;
            try
            {
                if (Config.GetBoolean(Config.Blurred, true) && ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.AcrylicBrush"))
                {
                    var source = Microsoft.UI.Xaml.Media.AcrylicBackgroundSource.Backdrop;
                    var acrylic = Color.FromArgb(128, sidebarColor.R, sidebarColor.G, sidebarColor.B);
                    brush = new Microsoft.UI.Xaml.Media.AcrylicBrush
                    {
                        FallbackColor = sidebarColor,
                        BackgroundSource = source,
                        TintColor = acrylic,
                        TintOpacity = 0.55
                    };
                }
                else
                {
                    brush = new SolidColorBrush(sidebarColor);
                }
            }
            catch (Exception e)
            {
                brush = new SolidColorBrush(sidebarColor);
            }

            BottomFillArea.Fill = brush;

            if (this.Window != null)
            {
                var bg = Config.GetBoolean(Config.DarkTheme) ? ColorUtils.ParseColor("#FF111111") : ColorUtils.ParseColor("#FFEEEEEE");

                Window.TitleBar.ButtonBackgroundColor = Window.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                Window.TitleBar.ButtonForegroundColor = Window.TitleBar.ButtonInactiveForegroundColor = darktheme ? Colors.White : Colors.Black;
                Window.TitleBar.ForegroundColor = darktheme ? Colors.White : Colors.Black;
                this.Background = new SolidColorBrush(bg);
            }
        }

        private void CompactToggle_Click(object sender, RoutedEventArgs e)
        {
            if (CompactToggle.IsChecked.Value)
            {
                Window.Presenter.RequestPresentation(AppWindowPresentationKind.CompactOverlay);
            }
            else
            {
                Window.Presenter.RequestPresentation(AppWindowPresentationKind.Default);
            }
        }
    }
}
