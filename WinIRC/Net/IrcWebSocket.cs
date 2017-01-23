using System;
using System.Diagnostics;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Web;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.UI.Popups;

namespace WinIRC.Net
{
    public class IrcWebSocket : Irc
    {
        private MessageWebSocket messageWebSocket;

        public override async void Connect()
        {
            // Make a local copy to avoid races with Closed events.
            MessageWebSocket webSocket = messageWebSocket;
            try
            {
                var protectionLevel = server.ssl ? "wss" : "ws";
                var socketUri = new Uri(protectionLevel + "://" + server.hostname + ":" + server.port + "/", UriKind.Absolute);
                Debug.WriteLine("Attempting to connect...");

                if (webSocket == null)
                {
                    Uri server = socketUri;

                    webSocket = new MessageWebSocket();

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

                Debug.WriteLine("Connected!");

                writer = new DataWriter(webSocket.OutputStream);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                var messageDialog = new MessageDialog("Could not connect to the server.");
                await messageDialog.ShowAsync();
                HandleDisconnect(this);
            }

        }

        private void Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            HandleDisconnect(this);
        }

        public override void Disconnect(string msg = "Powered by WinIRC", bool attemptReconnect = false)
        {
            WriteLine("QUIT :" + msg);
            HandleDisconnect(this);

            messageWebSocket.Dispose();
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
                            AttemptAuth();
                            return;
                        }

                        if (IsAuthed)
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                HandleLine(receivedData);
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
