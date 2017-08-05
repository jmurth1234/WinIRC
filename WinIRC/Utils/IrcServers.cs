using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml.Controls;
using WinIRC.Net;
using WinIRC.Ui;

namespace WinIRC.Utils
{
    class IrcServers
    {
        private static IrcServers instance;

        private ObjectStorageHelper<List<IrcServer>> serversListOSH;
        private bool loaded = false;

        public ObservableCollection<IrcServer> Servers { get; set; } = new ObservableCollection<IrcServer>();

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

            foreach (IrcServer server in Servers)
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
                serversListOSH = new ObjectStorageHelper<List<IrcServer>>(StorageType.Roaming);
                var servers = await serversListOSH.LoadAsync(Config.ServersListStore);

                if (servers != null && servers.Count > 0)
                {
                    servers.ForEach(Servers.Add);
                }
                else
                {
                    Servers = new ObservableCollection<IrcServer>();
                    await SaveServers();
                }
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Your saved servers have been corrupted for some reason. Clearing them. \n\nError: " + e.Message);
                await dialog.ShowAsync();

                Servers = new ObservableCollection<IrcServer>();
                await SaveServers();
            }

            if (Servers == null)
            {
                Servers = new ObservableCollection<IrcServer>();
            }
            loaded = true;

            Servers.CollectionChanged += Servers_CollectionChanged;
        }

        private async Task MigrateIfNeeded()
        {
            var serversOSH = new ObjectStorageHelper<List<IrcServer>>(StorageType.Roaming);

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

        internal Irc CreateConnection(IrcServer ircServer)
        {
            Net.Irc irc;
            if (ircServer.webSocket)
                irc = new Net.IrcWebSocket(ircServer);
            else
                irc = new Net.IrcSocket(ircServer);

            return irc;
        }

        public async Task AddServer(IrcServer server)
        {
            if (Servers == null)
            {
                Servers = new ObservableCollection<IrcServer>();
            }

            foreach (var serverCheck in Servers)
            {
                if (serverCheck.name == server.name)
                {
                    var name = serverCheck.name;
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
                if (ircServer.name == name)
                {
                    Servers.Remove(ircServer);
                    break;
                }
            }
            await SaveServers();
        }

        public async Task SaveServers()
        {
            var servers = new List<IrcServer>(Servers);
            await serversListOSH.SaveAsync(servers, Config.ServersListStore);
        }

        public IrcServer Get(String name)
        {
            if (!Servers.Any(server => server.name == name)) return new IrcServer();
            return Servers.First(server => server.name == name);
        }
    }
}
