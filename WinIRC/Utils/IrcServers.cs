using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using WinIRC.Net;

namespace WinIRC.Utils
{
    class IrcServers
    {
        private static IrcServers instance;

        private ObjectStorageHelper<List<IrcServer>> serversListOSH;

        public ObservableCollection<String> servers {
            get
            {
                var list = new ObservableCollection<string>();

                serversList.ForEach(server => list.Add(server.name));

                return list;
            }
        }
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
            try
            {
                serversListOSH = new ObjectStorageHelper<List<Net.IrcServer>>(StorageType.Roaming);
                serversList = await serversListOSH.LoadAsync(Config.ServersListStore);
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Your saved servers have been corrupted for some reason. Clearing them. \n\nError: " + e.Message);
                await dialog.ShowAsync();

                serversList = new List<IrcServer>();
                await serversListOSH.SaveAsync(serversList, Config.ServersListStore);
            }

            if (serversList == null)
            {
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
                    break;
                }
            }

            serversList.Add(server);

            serversListOSH.SaveAsync(serversList, Config.ServersListStore);
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
