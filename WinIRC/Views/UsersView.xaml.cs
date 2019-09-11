using IrcClientCore;
using IrcClientCore.Commands;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UsersView : Page
    {
        public string UserSelected { get; set; }
        private int UserClickBehaviour = 0;
        private CommandManager commands;
        private string channel;

        public UsersView()
        {
            this.InitializeComponent();
        }

        public void UpdateUsers(ObservableCollection<User> users)
        {
            this.usersList.ItemsSource = users;
            commands = MainPage.instance.GetCurrentServer().CommandManager;
            channel = MainPage.instance.currentChannel;

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
        }

        private void SendPrivateMessage()
        {
            commands.HandleCommand(channel, "/query " + UserSelected);
        }
    }
}
