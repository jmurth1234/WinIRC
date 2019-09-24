using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Template10.Common;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage.Streams;
using Windows.UI.Notifications;

namespace WinIRC.Net
{
    public class IrcSocket : IrcUWPBase
    {
        private const int socketReceiveBufferSize = 1024;
        private IBackgroundTaskRegistration task = null;
        private DataReaderLoadOperation readOperation;
        private SafeLineReader dataStreamLineReader;
        private CancellationTokenSource socketCancellation;


        public IrcSocket(WinIrcServer server) : base(server) {}

        public override async void Connect()
        {
            var autoReconnect = Config.GetBoolean(Config.AutoReconnect, true);

            if (Server == null)
                return;

            try
            {
                foreach (var current in BackgroundTaskRegistration.AllTasks)
                {
                    if (current.Value.Name == BackgroundTaskName)
                    {
                        task = current.Value;
                        break;
                    }
                }

                if (task == null)
                {
                    var socketTaskBuilder = new BackgroundTaskBuilder();
                    socketTaskBuilder.Name = "WinIRCBackgroundTask." + Server.Name;

                    var trigger = new SocketActivityTrigger();
                    socketTaskBuilder.SetTrigger(trigger);
                    //task = socketTaskBuilder.Register();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            IsAuthed = false;
            ReadOrWriteFailed = false;

            if (!ConnCheck.HasInternetAccess)
            {
                var msg = autoReconnect
                    ? "We'll try to connect once a connection is available." 
                    : "Please try again once your connection is restored";

                var error = IrcUWPBase.CreateBasicToast("No connection detected.", msg);
                error.ExpirationTime = DateTime.Now.AddDays(2);
                ToastNotificationManager.CreateToastNotifier().Show(error);

                if (!autoReconnect)
                {
                    DisconnectAsync(attemptReconnect: autoReconnect);
                }

                return;
            }

            streamSocket = new StreamSocket();
            streamSocket.Control.KeepAlive = true;

            if (Config.GetBoolean(Config.IgnoreSSL))
            {
                streamSocket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                streamSocket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            }

            if (task != null) streamSocket.EnableTransferOwnership(task.TaskId, SocketActivityConnectedStandbyAction.Wake);

            dataStreamLineReader = new SafeLineReader();

            try
            { 
                var protectionLevel = Server.Ssl ? SocketProtectionLevel.Tls12 : SocketProtectionLevel.PlainSocket;
                Debug.WriteLine("Attempting to connect...");
                await streamSocket.ConnectAsync(new Windows.Networking.HostName(Server.Hostname), Server.Port.ToString(), protectionLevel);
                Debug.WriteLine("Connected!");

                reader = new DataReader(streamSocket.InputStream);
                writer = new DataWriter(streamSocket.OutputStream);

                IsConnected = true;
                IsConnecting = false;

                ConnectionHandler();
            }
            catch (Exception e)
            {
                var msg = autoReconnect
                    ? "Attempting to reconnect..."
                    : "Please try again later.";

                AddError("Error whilst connecting: " + e.Message + "\n" + msg);
                AddError(e.StackTrace);
                AddError("If this error keeps occuring, ensure your connection settings are correct.");

                DisconnectAsync(attemptReconnect: autoReconnect);

                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private async void ConnectionHandler()
        {
            // while loop to keep the connection open
            while (IsConnected)
            {
                if (Transferred) { await Task.Delay(1); continue; }
                await ReadFromServer(reader, writer);
            }
        }

        public override async void SocketTransfer()
        {
            if (streamSocket == null) return;
            try
            {
                // set a variable to ensure the while loop doesn't continue trying to process connections
                Transferred = true;

                // detatch all the buffers
                await streamSocket.CancelIOAsync();

                // transfer the socket
                streamSocket.TransferOwnership(Server.Name);
                streamSocket = null;
            }
            catch (Exception e)
            {
                var toast = CreateBasicToast("Error when activating background socket", e.Message);
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
        }

        public override void SocketReturn()
        {
            SocketActivityInformation socketInformation;
            if (SocketActivityInformation.AllSockets.TryGetValue(Server.Name, out socketInformation))
            {
                // take the socket back, and hook up all the readers and writers so we can do stuff again
                streamSocket = socketInformation.StreamSocket;
                reader = new DataReader(streamSocket.InputStream);
                writer = new DataWriter(streamSocket.OutputStream);

                Transferred = false;
            }
        }

        public async Task ReadFromServer(DataReader reader, DataWriter writer)
        {
            // set the DataReader to only wait for available data
            reader.InputStreamOptions = InputStreamOptions.Partial;
            if (!IsAuthed)
            {
                AttemptAuth();
            }
            else
            {
                try
                {
                    await reader.LoadAsync(socketReceiveBufferSize);

                    while (reader.UnconsumedBufferLength > 0)
                    {
                        bool breakLoop = false;
                        byte readChar;
                        do
                        {
                            if (reader.UnconsumedBufferLength > 0)
                                readChar = reader.ReadByte();
                            else
                            {
                                breakLoop = true;
                                break;
                            }
                        } while (!dataStreamLineReader.Add(readChar));

                        if (breakLoop)
                            return;

                        // Read next line from data stream.
                        var line = dataStreamLineReader.SafeFlushLine();

                        if (line == null) break;
                        if (line.Length == 0) continue;

                        await RecieveLine(line);
                    }
                }
                catch (Exception e)
                {
                    AddError("Error with connection: " + e.Message);
                    AddError(e.StackTrace);
                    ReadOrWriteFailed = true;
                    IsConnected = false;

                    if (Server != null)
                        DisconnectAsync(attemptReconnect: Config.GetBoolean(Config.AutoReconnect));

                    return;
                }
            }
        }

        public override async void DisconnectAsync(string msg = "Powered by WinIRC", bool attemptReconnect = false)
        {
            WriteLine("QUIT :" + msg);
            IsConnected = false;

            if (attemptReconnect)
            {
                IsConnecting = true;
                ReconnectionAttempts++;
                if (ConnCheck != null && Server != null && ConnCheck.HasInternetAccess)
                {
                    await WindowWrapper.Current().Dispatcher.DispatchAsync(async () => {
                        if (ReconnectionAttempts < 3)
                            await Task.Delay(1000);
                        else
                            await Task.Delay(60000);

                        if (IsReconnecting)
                            Connect();
                    });
                }
            }
            else
            {
                IsConnecting = false;
                HandleDisconnect?.Invoke(this);
            }
        }
    }

    // Reads lines from text sources safely; unterminated lines are not returned.
    internal class SafeLineReader
    {
        // Current incomplete line;
        private string currentLine;

        private List<byte> bytesList = new List<byte>();

        private bool endOfLine = false;

        private char PreviousCharacter()
        {
            return currentLine[currentLine.Length - 1];
        }

        public bool Add(byte b)
        {
            char character = (char)b;
            if (character == '\n' && PreviousCharacter() == '\r')
                endOfLine = true;

            bytesList.Add(b);

            currentLine += character;

            return endOfLine;
        }

        public string FlushLine()
        {
            var buffer = bytesList.ToArray();
            currentLine = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            string tempLine = currentLine.Substring(0, currentLine.Length - 2);

            currentLine = String.Empty;
            endOfLine = false;
            bytesList.Clear();

            return tempLine;
        }

        public string SafeFlushLine()
        {
            if (endOfLine)
                return FlushLine();
            else
                return null;
        }
    }


}
