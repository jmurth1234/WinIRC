using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace WinIRC.Ui
{
    public sealed partial class ChannelListItem : UserControl
    {
        internal static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ChannelListItem), new PropertyMetadata(null));

        public event EventHandler ChannelCloseClicked;

        public event EventHandler ChannelJoinClicked;

        public event EventHandler ServerRightClickEvent;

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public ChannelListItem()
        {
            this.InitializeComponent();

            Loaded += (sender, args) =>
            {
                if (Title == "Server")
                {
                    CloseButton.Visibility = Visibility.Collapsed;
                    AddButton.Visibility = Visibility.Visible;
                    MenuButton.Visibility = Visibility.Visible;
                }

            };

            (this.Content as FrameworkElement).DataContext = this;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Title != "Server")
                ChannelCloseClicked?.Invoke(sender, new ChannelEventArgs(Title));
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (Title == "Server")
                ChannelJoinClicked?.Invoke(sender, new ChannelEventArgs(channel.Text));
        }

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ShowServerMenu(sender, e);
        }

        private void ShowServerMenu(object sender, RoutedEventArgs e)
        {
            if (Title == "Server")
            {
                var RightClick = this.Resources["ServerContextMenu"] as MenuFlyout;

                if (e is RightTappedRoutedEventArgs)
                    RightClick.ShowAt(null, (e as RightTappedRoutedEventArgs).GetPosition(null));
                else
                    RightClick.ShowAt(sender as FrameworkElement);

                Style s = new Windows.UI.Xaml.Style { TargetType = typeof(MenuFlyoutPresenter) };
                s.Setters.Add(new Setter(RequestedThemeProperty, Config.GetBoolean(Config.DarkTheme) ? ElementTheme.Dark : ElementTheme.Light));
                RightClick.MenuFlyoutPresenterStyle = s;
            }
        }

        private void CloseItem_Click(object sender, RoutedEventArgs e)
        {
            ServerRightClickEvent?.Invoke(sender, new ServerRightClickArgs(ServerRightClickType.CLOSE));
        }

        private void ReconnectItem_Click(object sender, RoutedEventArgs e)
        {
            ServerRightClickEvent?.Invoke(sender, new ServerRightClickArgs(ServerRightClickType.RECONNECT));
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            ShowServerMenu(sender, e);
        }
    }

    public enum ServerRightClickType
    {
        CLOSE, RECONNECT
    }

    public class ServerRightClickArgs : EventArgs
    {
        public ServerRightClickType type { get; private set; }

        public ServerRightClickArgs(ServerRightClickType type)
        {
            this.type = type;
        }
    }

    public class ChannelEventArgs : EventArgs
    {
        public ChannelEventArgs(string channel)
        {
            this.Channel = channel;
        } 

        public string Channel { get; private set; }
    }
}
