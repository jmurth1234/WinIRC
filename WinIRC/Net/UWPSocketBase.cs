using IrcClientCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Common;
using Windows.ApplicationModel.Core;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace WinIRC.Net
{
    public abstract class UWPSocketBase : ISocket
    {
        internal StreamSocket streamSocket;
        internal DataReader reader;
        internal DataWriter writer;

        public string BackgroundTaskName
        {
            get
            {
                return "WinIRCBackgroundTask." + Server.Name;
            }
        }

        internal Irc parent;

        internal IrcServer Server => parent.Server;

        internal Connection ConnCheck;

        public UWPSocketBase(Irc parent)
        {
            ConnCheck = new Connection();
            ConnCheck.ConnectionChanged += async (connected) =>
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ConnectionChanged(connected)
            );

            this.parent = parent;
        }

        private void ConnectionChanged(bool connected)
        {
            if (connected && Config.GetBoolean(Config.AutoReconnect))
            {
                foreach (Channel channel in parent.ChannelList)
                {
                    channel.ClientMessage("Reconnecting...");
                }
                Connect();
            }
            else
            {
                foreach (Channel channel in parent.ChannelList)
                {
                    channel.ClientMessage("Disconnected from IRC");
                }
                Disconnect(attemptReconnect: true);
            }
        }


        public async Task WriteLine(string str)
        {
            Debug.WriteLine(str);

            await WriteLine(writer, str);
        }

        public async Task WriteLine(DataWriter writer, string str)
        {
            if (parent.ReadOrWriteFailed)
                return;

            try
            {
                if (ConnCheck.HasInternetAccess && !parent.IsReconnecting)
                {
                    writer.WriteString(str + "\n");
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }
            }
            catch (Exception e)
            {
                parent.ReadOrWriteFailed = true;
                var autoReconnect = Config.GetBoolean(Config.AutoReconnect);

                var msg = autoReconnect
                    ? "Attempting to reconnect..."
                    : "Please try again later.";

                parent.AddError("Error whilst connecting: " + e.Message + "\n" + msg);
                parent.AddError(e.StackTrace);

                await Disconnect(attemptReconnect: autoReconnect);

                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        public abstract Task Connect();

        public async Task Disconnect(string msg = "Powered by WinIRC", bool attemptReconnect = false)
        {
            WriteLine("QUIT :" + msg);
            parent.IsConnected = false;

            if (attemptReconnect)
            {
                parent.IsConnecting = true;
                parent.ReconnectionAttempts++;
                if (ConnCheck != null && Server != null && ConnCheck.HasInternetAccess)
                {
                    await WindowWrapper.Current().Dispatcher.DispatchAsync(async () => {
                        if (parent.ReconnectionAttempts < 3)
                            await Task.Delay(1000);
                        else
                            await Task.Delay(60000);

                        if (parent.IsReconnecting)
                            await Connect();
                    });
                }
            }
            else
            {
                parent.IsConnecting = false;
                parent.HandleDisconnect?.Invoke(parent);
            }
        }

        public void Dispose()
        {
            try
            {

                if (reader != null) reader.Dispose();
                if (writer != null) writer.Dispose();

                if (streamSocket != null) streamSocket.Dispose();

                ConnCheck.ConnectionChanged = null;
                ConnCheck = null;
            }
            catch (NullReferenceException e)
            {
                // catching silently, sorry
            }
        }

    }
}
