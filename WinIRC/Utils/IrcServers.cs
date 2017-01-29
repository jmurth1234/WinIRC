using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.StartScreen;
using WinIRC.Net;

namespace WinIRC.Utils
{
    class IrcServers
    {
        private static IrcServers instance;

        private ObjectStorageHelper<ObservableCollection<string>> serversOSH;
        private ObjectStorageHelper<List<IrcServer>> serversListOSH;

        public ObservableCollection<String> servers { get; set; }
        public List<IrcServer> serversList { get; set; }

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

        private IrcServers()
        {
            loadServers();
        }

        public async Task UpdateJumpList()
        {
            JumpList jumpList = await JumpList.LoadCurrentAsync();
            jumpList.SystemGroupKind = JumpListSystemGroupKind.None;
            jumpList.Items.Clear();

            foreach (String server in servers)
            {
                JumpListItem item = JumpListItem.CreateWithArguments(server, server);
                item.GroupName = "Servers";
                item.Logo = new Uri("ms-appx:///Assets/winirc-jumplist.png");
                jumpList.Items.Add(item);
            }

            await jumpList.SaveAsync();
        }

        public async Task loadServers()
        {
            serversOSH = new ObjectStorageHelper<ObservableCollection<String>>(StorageType.Roaming);
            servers = await serversOSH.LoadAsync(Config.ServersStore);

            serversListOSH = new ObjectStorageHelper<List<Net.IrcServer>>(StorageType.Roaming);
            serversList = await serversListOSH.LoadAsync(Config.ServersListStore);

            if (servers == null)
            {
                servers = new ObservableCollection<string>();
                serversList = new List<IrcServer>();
            }
            servers.CollectionChanged += async (e, i) => await UpdateJumpList();
        }

        internal Irc CreateConnection(IrcServer ircServer)
        {
            Net.Irc irc;
            if (ircServer.webSocket)
                irc = new Net.IrcWebSocket();
            else
                irc = new Net.IrcSocket();

            irc.server = ircServer;

            return irc;
        }

        public async Task AddServer(IrcServer server)
        {
            if (servers == null)
            {
                servers = new ObservableCollection<string>();
                serversList = new List<IrcServer>();
            }

            foreach (var serverCheck in serversList)
            {
                if (serverCheck.name == server.name)
                {
                    var name = serverCheck.name;
                    serversList.Remove(serverCheck);
                    servers.Remove(name);

                    await serversListOSH.SaveAsync(serversList, Config.ServersListStore);
                    await serversOSH.SaveAsync(servers, Config.ServersStore);
                    break;
                }
            }

            servers.Add(server.name);
            serversList.Add(server);

            serversListOSH.SaveAsync(serversList, Config.ServersListStore);
            serversOSH.SaveAsync(servers, Config.ServersStore);
        }

        public async void DeleteServer(String name)
        {
            foreach (var ircServer in serversList)
            {
                if (ircServer.name == name)
                {
                    serversList.Remove(ircServer);
                    servers.Remove(name);

                    await serversListOSH.SaveAsync(serversList, Config.ServersListStore);
                    await serversOSH.SaveAsync(servers, Config.ServersStore);
                    break;
                }
            }
        }

        public IrcServer Get(String name)
        {
            if (!serversList.Any(server => server.name == name)) return new IrcServer();
            return serversList.First(server => server.name == name);
        }
    }
}
