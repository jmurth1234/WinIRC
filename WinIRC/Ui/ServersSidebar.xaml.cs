using IrcClientCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Net;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WinIRC.Ui
{
    public sealed partial class ServersSidebar : UserControl, INotifyPropertyChanged
    {

        public static readonly DependencyProperty ServerContentsProperty = DependencyProperty.Register(
            "ServerContents",
            typeof(ICollectionView),
            typeof(ServersSidebar),
            new PropertyMetadata(null)
        );

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICollectionView ServerContents
        {

            get
            {
                return (ICollectionView)GetValue(ServerContentsProperty);
            }
            set
            {
                SetValue(ServerContentsProperty, value);
                NotifyPropertyChanged();
            }
        }

        public object SelectedValue
        {
            get => channelList.SelectedValue;
            set => channelList.SelectedValue = value;
        }

        public ServersSidebar()
        {
            this.InitializeComponent();
        }
        private void ChannelListItem_ServerClickEvent(object sender, EventArgs e)
        {
            var header = sender as Ui.ChannelListItem;

            MainPage.instance.SwitchChannel(header.Server.Server.Name, "Server", false);
        }


        private void ChannelList_ItemClick(object sender, ItemClickEventArgs e)
        {
            channelList.SelectedItem = e.ClickedItem;
            try
            {
                if (channelList.SelectedItem == null)
                    return;

                MainPage.instance.SwitchChannel(channelList.SelectedItem as Channel);
            }
            catch (Exception ex)
            {
                var toast = IrcUWPBase.CreateBasicToast(ex.Message, ex.StackTrace);
                toast.ExpirationTime = DateTime.Now.AddDays(2);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
                Debug.WriteLine(ex);
            }
        }

        private void ChannelListItem_ChannelCloseClicked(object sender, EventArgs e)
        {
            var channelArgs = e as ChannelEventArgs;
            var channel = channelArgs.Channel;

            MainPage.instance.GetServer(channelArgs.Server).PartChannel(channel);
        }

        private void ChannelListItem_ChannelJoinClicked(object sender, EventArgs e)
        {
            var channelArgs = e as ChannelEventArgs;
            var channel = channelArgs.Channel;

            MainPage.instance.GetServer(channelArgs.Server).JoinChannel(channel);
        }

        private void ChannelListItem_ServerRightClickEvent(object sender, EventArgs e)
        {
            var args = e as ServerRightClickArgs;
            var server = MainPage.instance.GetServer(args.server);

            if (args.type == ServerRightClickType.RECONNECT)
                server.DisconnectAsync(attemptReconnect: true);
            else if (args.type == ServerRightClickType.DISCONNECT)
                server.DisconnectAsync(attemptReconnect: false);
            else if (args.type == ServerRightClickType.CLOSE)
                MainPage.instance.CloseServer(server);
        }

        internal void UpdateUi()
        {
            if (Config.Contains(Config.ReducedPadding))
            {
                int height;

                if (Config.GetBoolean(Config.ReducedPadding))
                {
                    height = 28;
                }
                else
                {
                    height = 42;
                }

                var res = new ResourceDictionary { Source = new Uri("ms-appx:///Styles/Styles.xaml", UriKind.Absolute) };

                var style = res["ListBoxItemStyle"] as Style;

                foreach (var item in style.Setters.Cast<Setter>().Where(item => item.Property == HeightProperty))
                    style.Setters.Remove(item);

                style.Setters.Add(new Setter(HeightProperty, height));

                this.channelList.ItemContainerStyle = style;
            }
        }
    }
}
