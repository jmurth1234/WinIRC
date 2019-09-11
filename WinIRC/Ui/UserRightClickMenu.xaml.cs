using IrcClientCore;
using IrcClientCore.Commands;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace WinIRC.Ui
{
    public partial class UserRightClickMenu
    {
        private CommandManager commands;
        private string channel;

        public string UserSelected { get; set; }
        public int UserClickBehaviour { get; private set; }

        public UserRightClickMenu()
        {
            this.InitializeComponent();

            if (Config.Contains(Config.UserListClick))
            {
                UserClickBehaviour = Config.GetInt(Config.UserListClick);
            }
        }

        private void MenuFlyout_Opened(object sender, object e)
        {
            commands = MainPage.instance.GetCurrentServer().CommandManager;
            channel = MainPage.instance.currentChannel;

            UsernameItem.Text = UserSelected;
        }

        private void UsernameItem_Click(object sender, RoutedEventArgs e)
        {
            var msgEntry = MainPage.instance.GetInputBox();
            if (msgEntry == null) return;

            if (msgEntry.Text == "")
            {
                msgEntry.Text = UserSelected + ": ";
            }
            else
            {
                msgEntry.Text += UserSelected + " ";
            }
        }

        private string GetUser(RoutedEventArgs e)
        {
            var user = "";
            if (e.OriginalSource is TextBlock)
            {
                TextBlock selectedItem = (TextBlock)e.OriginalSource;

                if (selectedItem.DataContext is User)
                {
                    user = ((User)selectedItem.DataContext).Nick;
                }

                if (selectedItem.DataContext is Message)
                {
                    user = ((Message)selectedItem.DataContext).User;
                }
            }
            else if (e.OriginalSource is ListViewItemPresenter)
            {
                ListViewItemPresenter selectedItem = (ListViewItemPresenter)e.OriginalSource;
                user = ((User)selectedItem.DataContext).Nick;
            }

            return user;
        }

        public void User_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            UserSelected = GetUser(e).Replace("@", "").Replace("+", "");

            ShowContextMenu(null, e.GetPosition(null));

            e.Handled = true;
        }

        private void ShowContextMenu(UIElement target, Point offset)
        {
            if (UserSelected == "") return;

            var RightClick = this["UserContextMenu"] as MenuFlyout;

            System.Diagnostics.Debug.WriteLine("MenuFlyout shown '{0}', '{1}'", target, offset);

            RightClick.ShowAt(target, offset);

            Style s = new Windows.UI.Xaml.Style { TargetType = typeof(MenuFlyoutPresenter) };
            s.Setters.Add(new Setter(FrameworkElement.RequestedThemeProperty, Config.GetBoolean(Config.DarkTheme) ? ElementTheme.Dark : ElementTheme.Light));
            RightClick.MenuFlyoutPresenterStyle = s;
        }

        public void User_Tapped(object sender, TappedRoutedEventArgs e)
        {
            UserSelected = GetUser(e).Replace("@", "").Replace("+", "");

            if (Config.Contains(Config.UserListClick))
            {
                UserClickBehaviour = Config.GetInt(Config.UserListClick);
            }

            if (UserClickBehaviour == 0)
            {
                var msgEntry = MainPage.instance.GetInputBox();

                if (msgEntry == null) return;

                if (msgEntry.Text == "")
                {
                    msgEntry.Text = UserSelected + ": ";
                }
                else
                {
                    msgEntry.Text += UserSelected + " ";
                }
            }
            else if (UserClickBehaviour == 1)
            {
                SendPrivateMessage();
            }
            else if (UserClickBehaviour == 2)
            {
                ShowContextMenu(null, e.GetPosition(null));
            }
        }

        private void SendMessageItem_Click(object sender, RoutedEventArgs e)
        {
            SendPrivateMessage();
        }

        private void SendPrivateMessage()
        {
            commands.HandleCommand(channel, "/query " + UserSelected);
        }

        private void WhoisItem_Click(object sender, RoutedEventArgs e)
        {
            commands.HandleCommand(channel, "/whois " + UserSelected);
        }

        private void OpItem_Click(object sender, RoutedEventArgs e)
        {
            commands.HandleCommand(channel, "/op " + UserSelected);
        }

        private void DeopItem_Click(object sender, RoutedEventArgs e)
        {
            commands.HandleCommand(channel, "/deop " + UserSelected);
        }

        private void VoiceItem_Click(object sender, RoutedEventArgs e)
        {
            commands.HandleCommand(channel, "/voice " + UserSelected);
        }

        private void DevoiceItem_Click(object sender, RoutedEventArgs e)
        {
            commands.HandleCommand(channel, "/devoice " + UserSelected);
        }

        private void MuteItem_Click(object sender, RoutedEventArgs e)
        {
            commands.HandleCommand(channel, "/mute " + UserSelected);
        }

        private void UnmuteItem_Click(object sender, RoutedEventArgs e)
        {
            commands.HandleCommand(channel, "/unmute " + UserSelected);
        }

        private void KickItem_Click(object sender, RoutedEventArgs e)
        {
            commands.HandleCommand(channel, "/kick " + UserSelected);
        }

        private void BanItem_Click(object sender, RoutedEventArgs e)
        {
            commands.HandleCommand(channel, "/ban " + UserSelected);
            commands.HandleCommand(channel, "/kick " + UserSelected);
        }
    }
}
