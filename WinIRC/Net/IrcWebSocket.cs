using System;
using System.Diagnostics;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Web;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.UI.Popups;
using Windows.UI.Notifications;
using System.Threading.Tasks;
using IrcClientCore;

namespace WinIRC.Net
{
    public class IrcWebSocket : UWPSocketBase
    {
        private MessageWebSocket messageWebSocket;

        public IrcWebSocket(Irc irc) : base(irc) { }

        public override async Task Connect()
        {
            // Make a local copy to avoid races with Closed events.
            MessageWebSocket webSocket = messageWebSocket;
            try
            {
                var protectionLevel = Server.Ssl ? "wss" : "ws";
                var socketUri = new Uri(protectionLevel + "://" + Server.Hostname + ":" + Server.Port + "/", UriKind.Absolute);
                Debug.WriteLine("Attempting to connect...");

                if (webSocket == null)
                {
                    Uri server = socketUri;

                    webSocket = new MessageWebSocket();

                    if (Config.GetBoolean(Config.IgnoreSSL))
                        webSocket.ServerCustomValidationRequested += WebSocket_ServerCustomValidationRequested;

                    // MessageWebSocket supports both utf8 and binary messages.
                    // When utf8 is specified as the messageType, then the developer
                    // promises to only send utf8-encoded data.
                    webSocket.Control.MessageType = SocketMessageType.Utf8;
                    // Set up callbacks
                    webSocket.MessageReceived += MessageReceived;
                    webSocket.Closed += Closed;

                    await webSocket.ConnectAsync(server);
                    messageWebSocket = webSocket; // Only store it after successfully connecting.
                }

                parent.IsConnected = true;
                parent.IsConnecting = false;

                Debug.WriteLine("Connected!");

                writer = new DataWriter(webSocket.OutputStream);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                var messageDialog = new MessageDialog("Could not connect to the server.");
                await messageDialog.ShowAsync();
                parent.HandleDisconnect(parent);
            }

        }

        private void WebSocket_ServerCustomValidationRequested(MessageWebSocket sender, WebSocketServerCustomValidationRequestedEventArgs args)
        {
            // allowed
        }

        private void Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            var autoReconnect = Config.GetBoolean(Config.AutoReconnect);

            var msg = autoReconnect
                ? "Attempting to reconnect..."
                : "Please try again later.";

            var error = IrcUWPBase.CreateBasicToast("Websocket closed", msg);

            ToastNotificationManager.CreateToastNotifier().Show(error);

            Disconnect(attemptReconnect: autoReconnect);
        }

        private async void MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            // container for the received Data
            // in this case a string
            string receivedData = "";
            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    var count = reader.UnconsumedBufferLength;
                    // read the data as a string and store it in our container
                    if (count > 0)
                    {
                        receivedData = reader.ReadString(count);

                        if (receivedData == "SOCKET_CONNECTED_IRC")
                        {
                            parent.AttemptAuth();
                            return;
                        }

                        if (parent.IsAuthed)
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                parent.RecieveLine(receivedData);
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WebErrorStatus status = WebSocketError.GetStatus(e.GetBaseException().HResult);
                Debug.WriteLine("Error with recieving a message: " + status);
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }
    }
}
