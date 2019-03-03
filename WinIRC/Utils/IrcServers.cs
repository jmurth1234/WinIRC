using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml.Controls;
using WinIRC.Net;
using WinIRC.Ui;
using System.IO;

namespace WinIRC.Utils
{
    class IrcServers
    {
        private static IrcServers instance;

        private ObjectStorageHelper<List<WinIrcServer>> serversListOSH;
        private bool loaded = false;

        public ObservableCollection<WinIrcServer> Servers { get; set; } = new ObservableCollection<WinIrcServer>();

        public static IrcServers Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new IrcServers();
                }
                return instance;
            }
        }

        public int Count
        {
            get => Servers.Count;
        }

        private IrcServers()
        {
        }

        public async Task UpdateJumpList()
        {
            if (Servers == null)
            {
                await loadServersAsync();
            }

            JumpList jumpList = await JumpList.LoadCurrentAsync();
            jumpList.SystemGroupKind = JumpListSystemGroupKind.None;
            jumpList.Items.Clear();

            foreach (WinIrcServer server in Servers)
            {
                JumpListItem item = JumpListItem.CreateWithArguments(server.ToString(), server.ToString());
                item.GroupName = "Servers";
                item.Logo = new Uri("ms-appx:///Assets/winirc-jumplist.png");
                jumpList.Items.Add(item);
            }

            await jumpList.SaveAsync();
        }

        public async Task loadServersAsync()
        {
            if (loaded)
            {
                return;
            }

            try
            {
                await MigrateIfNeeded();
                serversListOSH = new ObjectStorageHelper<List<WinIrcServer>>(StorageType.Roaming);
                await ConvertIfNeeded();
                var servers = await serversListOSH.LoadAsync(Config.ServersListStore);

                if (servers != null && servers.Count > 0)
                {
                    servers.ForEach(Servers.Add);
                }
                else
                {
                    Servers = new ObservableCollection<WinIrcServer>();
                    await SaveServers();
                }
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Your saved servers have been corrupted for some reason. Clearing them. \n\nError: " + e.Message);
                await dialog.ShowAsync();

                Servers = new ObservableCollection<WinIrcServer>();
                await SaveServers();
            }

            if (Servers == null)
            {
                Servers = new ObservableCollection<WinIrcServer>();
            }
            loaded = true;

            Servers.CollectionChanged += Servers_CollectionChanged;
        }

        private async Task ConvertIfNeeded()
        {
            var folder = serversListOSH.GetFolder(StorageType.Roaming);
            var file = await serversListOSH.GetFileIfExistsAsync(folder, Config.ServersListStore);
            var stream = await file.OpenReadAsync();
            XDocument loadedData = XDocument.Load(stream.AsStream());

            if ((await folder.GetItemsAsync()).Count == 0 && !(await serversListOSH.FileExists(folder, "converted")))
            {
                await folder.CreateFileAsync("converted", CreationCollisionOption.FailIfExists);
                return;
            }

            if (!(await serversListOSH.FileExists(folder, "converted")))
            {
                var data = from query in loadedData.Descendants("IrcServer")
                           select new WinIrcServer
                           {
                               Name = (string)query.Element("name"),
                               Hostname = (string)query.Element("hostname"),
                               Port = (int)query.Element("port"),
                               Ssl = (bool)query.Element("ssl"),
                               webSocket = (bool)query.Element("webSocket"),
                               Username = (string)query.Element("username"),
                               Password = (string)query.Element("password"),
                               NickservPassword = (string)query.Element("nickservPassword"),
                               Channels = (string)query.Element("channels")
                           };
                foreach (var server in data.ToArray())
                {
                    Servers.Add(server);
                }
                await folder.CreateFileAsync("converted", CreationCollisionOption.FailIfExists);

                await SaveServers();
            }
        }

        private async Task MigrateIfNeeded()
        {
            var serversOSH = new ObjectStorageHelper<List<WinIrcServer>>(StorageType.Roaming);

            var folder = serversOSH.GetFolder(StorageType.Roaming);

            if ((await folder.GetItemsAsync()).Count == 0 && !(await serversOSH.FileExists(folder, "migrated")))
            {
                await folder.CreateFileAsync("migrated", CreationCollisionOption.FailIfExists);
                return;
            }

            if ((await folder.GetItemsAsync()).Count == 1)
            {
                return;
            }

            if (!(await serversOSH.FileExists(folder, "migrated")))
            {
                var servers = await serversOSH.LoadAsync();
                await serversOSH.MigrateAsync(servers, Config.ServersStore);

                await folder.CreateFileAsync("migrated", CreationCollisionOption.FailIfExists);
            }

        }

        private async void Servers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            await UpdateJumpList();
        }

        internal IrcUWPBase CreateConnection(WinIrcServer ircServer)
        {
            Net.IrcUWPBase irc;
            if (ircServer.webSocket)
                irc = new Net.IrcWebSocket(ircServer);
            else
                irc = new Net.IrcSocket(ircServer);

            return irc;
        }

        public async Task AddServer(WinIrcServer server)
        {
            if (Servers == null)
            {
                Servers = new ObservableCollection<WinIrcServer>();
            }

            foreach (var serverCheck in Servers)
            {
                if (serverCheck.Name == server.Name)
                {
                    var name = serverCheck.Name;
                    Servers.Remove(serverCheck);
                    break;
                }
            }

            Servers.Add(server);
            await SaveServers();
        }

        public async void DeleteServer(String name)
        {
            foreach (var ircServer in Servers)
            {
                if (ircServer.Name == name)
                {
                    Servers.Remove(ircServer);
                    break;
                }
            }
            await SaveServers();
        }

        public async Task SaveServers()
        {
            var servers = new List<WinIrcServer>(Servers);
            await serversListOSH.SaveAsync(servers, Config.ServersListStore);
        }

        public WinIrcServer Get(String name)
        {
            if (!Servers.Any(server => server.Name == name)) return new WinIrcServer();
            return Servers.First(server => server.Name == name);
        }

        // this is only used here to convert old servers
        private class OldIrcServer
        {
            public bool invalid { get; set; }

            public string name { get; set; } = "";
            public string hostname { get; set; } = "";
            public int port { get; set; } = 6667;
            public bool ssl { get; set; } = false;
            public bool webSocket { get; set; } = false;
            public string username { get; set; } = "";
            public string password { get; set; } = "";
            public string nickservPassword { get; set; } = "";

            // channels are a string seperated by commas
            public string channels { get; set; } = "";

            public override String ToString()
            {
                return name;
            }
        }
    }

}
