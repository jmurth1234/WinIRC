using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Commands;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UsersView : Page
    {
        public String UserSelected;
        private int UserClickBehaviour = 0;
        private CommandHandler commands;

        public UsersView()
        {
            this.InitializeComponent();
        }

        public void UpdateUsers(ObservableCollection<string> users)
        {
            this.usersList.ItemsSource = users;
            commands = MainPage.instance.CommandHandler;

            if (Config.Contains(Config.ReducedPadding))
            {
                Thickness padding = new Thickness();
                padding.Left = 10;
                padding.Right = 10;
                if (Config.GetBoolean(Config.ReducedPadding))
                {
                    padding.Top = 4;
                    padding.Bottom = 4;
                }
                else
                {
                    padding.Top = 10;
                    padding.Bottom = 10;
                }

                var res = new ResourceDictionary { Source = new Uri("ms-appx:///Styles/Styles.xaml", UriKind.Absolute) };

                var style = res["ListViewItemStyle"] as Style;

                foreach (var item in style.Setters.Cast<Setter>().Where(item => item.Property == PaddingProperty))
                    style.Setters.Remove(item);

                foreach (var item in style.Setters.Cast<Setter>().Where(item => item.Property == MarginProperty))
                    style.Setters.Remove(item);


                style.Setters.Add(new Setter(PaddingProperty, padding));
                style.Setters.Add(new Setter(MarginProperty, padding));

                this.usersList.ItemContainerStyle = style;
            }

            if (Config.Contains(Config.UserListClick))
            {
                UserClickBehaviour = Config.GetInt(Config.UserListClick);
            }
        }

        private void UsersList_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var user = "";
            if (e.OriginalSource is TextBlock)
            {
                TextBlock selectedItem = (TextBlock)e.OriginalSource;

                user = selectedItem.Text;

            }
            else if (e.OriginalSource is ListViewItemPresenter)
            {
                ListViewItemPresenter selectedItem = (ListViewItemPresenter)e.OriginalSource;
                user = (string)selectedItem.Content;
            }
            else
            {
                return;
            }

            UserSelected = user.Replace("@", "").Replace("+", "");

            ShowContextMenu(null, e.GetPosition(null));

            e.Handled = true;

            base.OnRightTapped(e);
        }

        private void ShowContextMenu(UIElement target, Point offset)
        {
            var RightClick = this.Resources["UserContextMenu"] as MenuFlyout;

            System.Diagnostics.Debug.WriteLine("MenuFlyout shown '{0}', '{1}'", target, offset);

            RightClick.ShowAt(target, offset);

            Style s = new Windows.UI.Xaml.Style { TargetType = typeof(MenuFlyoutPresenter) };
            s.Setters.Add(new Setter(RequestedThemeProperty, Config.GetBoolean(Config.DarkTheme) ? ElementTheme.Dark : ElementTheme.Light));
            RightClick.MenuFlyoutPresenterStyle = s;

            UsernameItem.Text = UserSelected;
        }

        private void usersList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var user = "";
            if (e.OriginalSource is TextBlock)
            {
                TextBlock selectedItem = (TextBlock)e.OriginalSource;

                user = selectedItem.Text;

            }
            else if (e.OriginalSource is ListViewItemPresenter)
            {
                ListViewItemPresenter selectedItem = (ListViewItemPresenter)e.OriginalSource;
                user = (string)selectedItem.Content;
            }
            else
            {
                return;
            }

            UserSelected = user.Replace("@", "").Replace("+", "");

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

        private void SendMessageItem_Click(object sender, RoutedEventArgs e)
        {
            SendPrivateMessage();
        }

        private void SendPrivateMessage()
        {
            commands.QueryCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "QUERY", UserSelected });
        }

        private void WhoisItem_Click(object sender, RoutedEventArgs e)
        {
            commands.WhoisCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "WHOIS", UserSelected });
        }

        private void OpItem_Click(object sender, RoutedEventArgs e)
        {
            commands.OpCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "OP", UserSelected });
        }

        private void DeopItem_Click(object sender, RoutedEventArgs e)
        {
            commands.OpCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "DEOP", UserSelected });
        }

        private void VoiceItem_Click(object sender, RoutedEventArgs e)
        {
            commands.VoiceCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "VOICE", UserSelected });
        }

        private void DevoiceItem_Click(object sender, RoutedEventArgs e)
        {
            commands.VoiceCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "DEVOICE", UserSelected });
        }

        private void MuteItem_Click(object sender, RoutedEventArgs e)
        {
            commands.QuietCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "MUTE", UserSelected });
        }

        private void UnmuteItem_Click(object sender, RoutedEventArgs e)
        {
            commands.QuietCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "UNMUTE", UserSelected });

        }

        private void KickItem_Click(object sender, RoutedEventArgs e)
        {
            commands.KickCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "KICK", UserSelected });
        }

        private void BanItem_Click(object sender, RoutedEventArgs e)
        {
            commands.BanCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "BAN", UserSelected });
            commands.KickCommandHandler(MainPage.instance.GetCurrentServer(), new string[] { "KICK", UserSelected });

        }
    }

}
