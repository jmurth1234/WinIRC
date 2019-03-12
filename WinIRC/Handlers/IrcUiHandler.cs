using IrcClientCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using WinIRC.Net;

namespace WinIRC.Handlers
{
    // some decoupled code from the MainPage
    // This was originally to assist with the creation of a popout page but this feature was scrapped due to threads

    public class IrcUiHandler
    {
        public Dictionary<string, IrcUWPBase> connectedServers { get; set; }
        public ObservableCollection<string> connectedServersList { get; set; }
        public ObservableCollection<ChannelsGroup> Servers { get; set; }
        public static IrcUiHandler Instance;

        public IrcUiHandler ()
        {
            connectedServers = new Dictionary<string, IrcUWPBase>();
            connectedServersList = new ObservableCollection<string>();
            connectedServersList.CollectionChanged += ConnectedServersList_CollectionChanged;
            Servers = new ObservableCollection<ChannelsGroup>();

            // CommandHandler = new CommandHandler();

            Instance = this;
        }

        private void ConnectedServersList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var serverName = e.NewItems[0] as string;
                var server = connectedServers[serverName];
                Servers.Add(server.ChannelList);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var serverName = e.OldItems[0] as string;
                var server = Servers.First(s => s.Server == serverName);
                Servers.Remove(server);
            }
        }

        public void IrcTextBoxHandler(TextBox msgBox, KeyRoutedEventArgs e, string server, string channel)
        {
            if (server == null || channel == null || server == "" || channel == "")
            {
                return;
            }

            if ((e.Key == Windows.System.VirtualKey.Enter) && (msgBox.Text != ""))
            {
                connectedServers[server].CommandManager.HandleCommand(channel, msgBox.Text);

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

            var position = msgBox.SelectionStart;
            var words = msgBox.Text.Split(' ');
            var word = words[words.Length - 1];
            var isFirst = (words.Length == 1);
            var completions = GetTabCompletions(server, channel, msgBox.Text, word, words.Length - 1);

            if (word.Length == 0)
                return;

            foreach (var item in completions)
            {
                if (item.ToLower().StartsWith(word.ToLower()))
                {
                    if (isFirst && !word.StartsWith("/"))
                    {
                        msgBox.Text = item + ": ";
                    }
                    else
                    {
                        words[words.Length - 1] = words[words.Length - 1].Replace(word, item);
                        msgBox.Text = String.Join(" ", words) + " ";
                    }
                    msgBox.SelectionStart = msgBox.Text.Length;
                    msgBox.SelectionLength = 0;
                    msgBox.Focus(FocusState.Keyboard);
                    break;
                }
            }
        }

        public string[] GetTabCompletions(string server, string channel, string message, string current, int position)
        {
            var array = message.Split(' ');
            var Handler = connectedServers[server].CommandManager;
            if (message.StartsWith("/"))
            {
                if (array.Length > 1)
                {
                    var completions = Handler.GetCompletions(channel, array[0], current);
                    return completions.Length > 0 ? completions : GetUserCompletions(server, channel, current);
                }

                var commands = Handler.CommandList.Where(cmd => cmd.StartsWith(current));
                return commands.ToArray();
            }

            if ((message.StartsWith("/") && position > 0 || !message.StartsWith("/")) && channel != null)
            {
                return GetUserCompletions(server, channel, current);
            }

            return new string[0];
        }

        private string[] GetUserCompletions(string server, string channel, string word)
        {
            var users = connectedServers[server].ChannelList[channel].Store.RawUsers;
            return users.Where(cmd => cmd.StartsWith(word)).ToArray();
        }


        public void UpdateUsers(Frame frame, string server, string channel)
        {
            if (server == "" || channel == "" || !connectedServers.ContainsKey(server))
            {
                return;
            }

            var users = connectedServers[server].GetChannelUsers(channel);

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
