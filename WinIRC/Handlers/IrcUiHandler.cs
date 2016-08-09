using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using WinIRC.Commands;

namespace WinIRC.Handlers
{
    // some decoupled code from the MainPage
    // This was originally to assist with the creation of a popout page but this feature was scrapped due to threads

    public class IrcUiHandler
    {
        public Dictionary<string, Net.Irc> connectedServers { get; set; }
        public ObservableCollection<String> connectedServersList { get; set; }
        public CommandHandler CommandHandler { get; private set; }

        public static IrcUiHandler Instance;

        public IrcUiHandler ()
        {
            connectedServers = new Dictionary<string, Net.Irc>();
            connectedServersList = new ObservableCollection<string>();
            CommandHandler = new CommandHandler();

            Instance = this;
        }

        public void IrcTextBoxHandler(TextBox msgBox, KeyRoutedEventArgs e, string server, string channel)
        {
            if (server == null || channel == null || server == "" || channel == "")
            {
                return;
            }

            if ((e.Key == Windows.System.VirtualKey.Enter) && (msgBox.Text != ""))
            {
                CommandHandler.HandleCommand(connectedServers[server], msgBox.Text);

                msgBox.Text = "";
            }
            else if ((e.Key == Windows.System.VirtualKey.Tab) && (msgBox.Text != ""))
            {
                e.Handled = true;

                TabComplete(msgBox, server, channel);
            }
        }

        public void TabComplete(TextBox msgBox, string server, string channel)
        {
            if (server == null || channel == null || server == "" || channel == "")
            {
                return;
            }

            var users = connectedServers[server].GetRawUsers(channel);
            var words = msgBox.Text.Split(' ');
            var word = words[words.Length - 1];
            var isFirst = (words.Length == 1);

            if (word.Length == 0)
                return;

            foreach (var user in users)
            {
                if (user.ToLower().StartsWith(word.ToLower()))
                {
                    if (isFirst)
                    {
                        msgBox.Text = user + ": ";
                    }
                    else
                    {
                        words[words.Length - 1] = words[words.Length - 1].Replace(word, user);
                        msgBox.Text = String.Join(" ", words) + " ";
                    }
                    msgBox.SelectionStart = msgBox.Text.Length;
                    msgBox.SelectionLength = 0;
                    msgBox.Focus(FocusState.Keyboard);
                    break;
                }
            }
        }

        public void UpdateUsers(Frame frame, string server, string channel, bool clear = false)
        {
            if (server == "" || channel == "" || !connectedServers.ContainsKey(server))
            {
                return;
            }

            var users = new ObservableCollection<string>();

            if (!clear)
                users = connectedServers[server].GetChannelUsers(channel);

            if (!(frame.Content is UsersView))
            {
                frame.Navigate(typeof(UsersView));
            }

            var usersView = (UsersView)frame.Content;

            if (usersView != null)
                usersView.UpdateUsers(users);

        }

    }
}
