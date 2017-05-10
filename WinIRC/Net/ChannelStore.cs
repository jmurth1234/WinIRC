using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace WinIRC.Net
{
    public class ChannelStore
    {
        private CoreDispatcher dispatcher;
        private bool currentlySorting;

        public ObservableCollection<User> Users { get; private set; }
        public ObservableCollection<string> RawUsers { get; private set; }
        public ObservableCollection<string> SortedUsers { get; private set; }

        public string Topic { get; private set; }

        public ChannelStore()
        {
            this.Users = new ObservableCollection<User>();
            this.RawUsers = new ObservableCollection<string>();
            this.SortedUsers = new ObservableCollection<string>();
        }

        public void ClearUsers()
        {
            Users.Clear();
            SortedUsers.Clear();
        }

        public void SortUsers()
        {
            if (currentlySorting) return;
            currentlySorting = true;
            var watch = Stopwatch.StartNew();

            SortedUsers.Clear();
            var ops = new List<string>();
            var voiced = new List<string>();
            var users = new List<string>();

            foreach (var user in Users)
            {
                if (user.Prefix.StartsWith("@"))
                {
                    ops.Add("@" + user.Nick);
                }
                else if (user.Prefix.StartsWith("+"))
                {
                    voiced.Add("+" + user.Nick);
                }
                else
                {
                    users.Add(user.Nick);
                }
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Debug.WriteLine("Elapsed time to sort: " + elapsedMs + "ms");
            watch.Start();

            ops.Sort();
            voiced.Sort();
            users.Sort();

            ops.ForEach(SortedUsers.Add);
            voiced.ForEach(SortedUsers.Add);
            users.ForEach(SortedUsers.Add);

            watch.Stop();
            var elapsedMsOrder = watch.ElapsedMilliseconds;

            Debug.WriteLine("Elapsed time to order: " + elapsedMsOrder + "ms");

            Debug.WriteLine("Total time: " + (elapsedMsOrder + elapsedMs) + "ms");
            currentlySorting = false;

        }

        public void AddUsers(List<string> users)
        {
            users.ForEach(AddUser);
        }

        private void AddUser(string username)
        {
            AddUser(username, false);
        }

        public void AddUser(string username, bool sort)
        {
            if (username.Length == 0)
            {
                return;
            }

            if (!HasUser(username))
            {
                string prefix = "";

                if (username.Length > 2)
                {
                    prefix = username[0] + "" + username[1];
                }
                else if (username.Length > 1)
                {
                    prefix = username[0] + "";
                }
                User user = new User
                {
                    Nick = username.Replace("@", "").Replace("+", ""),
                    Prefix = prefix
                };

                RawUsers.Add(user.Nick);

                Users.Add(user);
                if (sort)
                    SortUsers();
            }
        }

        public Boolean HasUser(string nick)
        {
            nick = nick.Replace("@", "").Replace("+", "");
            if (nick == "") return false;

            return Users.Any(user => user.Nick == nick);
        }

        public void ChangeUser(string oldNick, string newNick) {
            if (!HasUser(oldNick)) return; 

            var user = Users.First(u => u.Nick == oldNick);

            RemoveUser(user.Nick);

            AddUser(user.Prefix + newNick, true);
        }

        public void ChangePrefix(string nick, string newPrefix)
        {
            if (!HasUser(nick)) return;

            var user = Users.First(u => u.Nick == nick);

            RemoveUser(nick);
            AddUser(newPrefix + user.Nick, true);
        }

        internal string GetPrefix(string user)
        {
            if (HasUser(user))
                return Users.First(u => u.Nick == user).Prefix;
            else return "";
        }

        public void RemoveUser(string nick)
        {
            if (Users.Any(user => user.Nick == nick))
            {
                var user = Users.First(u => u.Nick == nick);

                Users.Remove(user);
                RawUsers.Remove(user.Nick);

                SortUsers();
            }
        }

        public void SetTopic(string topic)
        {
            this.Topic = topic;
        }
    }

    public class User
    {
        public string Prefix { get; set; }
        public string Nick { get; set; }

        public override string ToString()
        {
            return Nick;
        }

    }

}
