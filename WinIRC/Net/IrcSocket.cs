using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Notifications;

namespace WinIRC.Net
{
    public class IrcSocket : Irc
    {
        public bool IsConnected = false;
        private SafeLineReader dataStreamLineReader;
        private const int socketReceiveBufferSize = 1024;
        private ControlChannelTrigger channel;

        public override async void Connect()
        {

            try
            {
                // TODO: Get this working  (probably after next update)
                channel = new ControlChannelTrigger(server.name, 2);
                channel.UsingTransport(streamSocket);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            streamSocket = new StreamSocket();
            streamSocket.SetIrc(this);
            streamSocket.Control.KeepAlive = true;
            try
            {
                var protectionLevel = server.ssl ? SocketProtectionLevel.Tls12 : SocketProtectionLevel.PlainSocket;
                Debug.WriteLine("Attempting to connect...");
                await streamSocket.ConnectAsync(new Windows.Networking.HostName(server.hostname), server.port.ToString(), protectionLevel);
                Debug.WriteLine("Connected!");

                reader = new DataReader(streamSocket.InputStream);
                writer = new DataWriter(streamSocket.OutputStream);
                dataStreamLineReader = new SafeLineReader();
                reader.InputStreamOptions = InputStreamOptions.Partial;
                IsConnected = true;

                // TODO: Get this working  (probably after next update)
                try
                {
                    var status = channel.WaitForPushEnabled();
                    if (status != ControlChannelTriggerStatus.HardwareSlotAllocated
                        && status != ControlChannelTriggerStatus.SoftwareSlotAllocated)
                    {
                        Debug.WriteLine(string.Format("Neither hardware nor software slot could be allocated. ChannelStatus is {0}", status.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }


                ConnectionHandler();
                

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                return;
            }

        }

        private async void ConnectionHandler()
        {
            // while loop to keep the connection open
            while (IsConnected)
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
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                        Disconnect();
                        break;
                    }

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
                            break;

                        // Read next line from data stream.
                        var line = dataStreamLineReader.SafeFlushLine();
                        if (line == null) break;
                        if (line.Length == 0) continue;
                        if (line.StartsWith("ERROR"))
                        {
                            Disconnect();
                            break;
                        }

                        HandleLine(line);
                    }
                }
            }
        }

        public override void Disconnect(string msg = "Powered by WinIRC")
        {
            WriteLine("QUIT :" + msg);
            IsConnected = false;
            HandleDisconnect(this);
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
            char character = (char) b;
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

    public static class StreamSocketExtension
    {
        private static Dictionary<StreamSocket, Irc> ircClients = new Dictionary<StreamSocket, Irc>();

        public static void SetIrc(this StreamSocket socket, Irc irc)
        {
            ircClients[socket] = irc;
        }

        public static Irc GetIrc(this StreamSocket socket)
        {
            return ircClients[socket];
        }

        public static void RemoveIrc(this StreamSocket socket)
        {
            ircClients.Remove(socket);
        }
    }
}
