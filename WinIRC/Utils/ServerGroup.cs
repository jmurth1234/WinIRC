using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinIRC.Net;

namespace WinIRC.Utils
{
    public class ServerGroup : ObservableCollection<Channel>
    {
        public ServerGroup(ObservableCollection<Channel> items) : base(items)
        {
        }

        private bool ServerAdded;

        public bool Contains(String s)
        {
            return this.Any(chan => chan.Name == s );
        }

        public void Add(String s)
        {
            this.Add(new Channel
            {
                Name = s,
                Server = Server
            });
        }

        public void Insert(int i, String s)
        {
            if (s == "Server" && !ServerAdded)
            {
                ServerAdded = true;
                return;
            }

            this.Insert(i, new Channel
            {
                Name = s,
                Server = Server
            });
        }

        public void Remove(String s)
        {
            var channel = this.First(chan => chan.Name == s);
            this.Remove(channel);
        }

        public string Server { get; set; }
    }

    public class Channel
    {
        public string Server { get; set; }
        public string Name { get; set; }
        public bool ServerLog => Name == "Server";

        public override string ToString()
        {
            return Name;
        }
    }
}
