using Microsoft.QueryStringDotNET;
using NotificationsExtensions;
using NotificationsExtensions.Toasts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;

namespace WinIRC.Net
{
    public class Irc
    {
        internal StreamSocket streamSocket;
        internal DataReader reader;
        internal DataWriter writer;

        public IrcServer server { get; set; }

        public String BackgroundTaskName {
            get
            {
                return "WinIRCBackgroundTask." + server.name;
            }
        }

        public bool IsAuthed { get; set; }

        public ObservableCollection<Message> ircMessages { get; set; }
        public ObservableCollection<string> channelList { get; set; }
        public Dictionary<string, ChannelStore> channelStore { get; set; }

        public Dictionary<string, ObservableCollection<Message>> channelBuffers { get; set; }

        Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

        private string currentWhois = "";
        private string[] WhoisCmds = new string[] { "311", "319", "312", "330", "671", "317", "401" };

        public string buffer;
        public string currentChannel;
        public bool Transferred = false;

        private string lightTextColor;
        private string chatTextColor;
        internal Connection ConnCheck;

        internal bool IsReconnecting;

        public bool IsConnected = false;
        internal int ReconnectionAttempts;
        internal bool ReadOrWriteFailed;

        public Action<Irc> HandleDisconnect { get; set; }

        public string Nickname {
            get
            {
                return server.username;
            }
            set
            {
                server.username = value;
                WriteLine("NICK " + value);

                foreach (string channel in channelBuffers.Keys)
                {
                    ClientMessage(channel, "Changed username to " + value);
                }
            }
        }

        public Irc()
        {
            ircMessages = new ObservableCollection<Message>();
            channelList = new ObservableCollection<string>();
            channelBuffers = new Dictionary<string, ObservableCollection<Message>>(StringComparer.OrdinalIgnoreCase);
            channelStore = new Dictionary<string, ChannelStore>(StringComparer.OrdinalIgnoreCase);
            IsAuthed = false;
            ConnCheck = new Connection();

            ConnCheck.ConnectionChanged += async (connected) =>
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ConnectionChanged(connected)
            );
        }

        private void ConnectionChanged(bool connected)
        {
            if (connected && Config.GetBoolean(Config.AutoReconnect))
            {
                foreach (string channel in channelBuffers.Keys)
                {
                    ClientMessage(channel, "Reconnecting...");
                }
                Connect();
            }
            else
            {
                foreach (string channel in channelBuffers.Keys)
                {
                    ClientMessage(channel, "Disconnected from IRC");
                }
                DisconnectAsync(attemptReconnect: true);
            }
        }

        public virtual async void Connect() { }
        public virtual async void DisconnectAsync(string msg = "Powered by WinIRC", bool attemptReconnect = false) { }
        public virtual async void SocketTransfer() { }
        public virtual async void SocketReturn() { }

        internal void AttemptAuth()
        {
            // Auth to the server
            Debug.WriteLine("Attempting to auth");

            WriteLine("CAP LS");

            AttemptRegister();

            if (server.password != "")
            {
                WriteLine("PASS " + server.password);
            }

            IsAuthed = true;
        }

        private void AttemptRegister()
        {
            try
            {
                WriteLine(String.Format("NICK {0}", server.username));
                WriteLine(String.Format("USER {0} {1} * :{2}", server.username, "8", server.username));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        internal async Task HandleLine(string receivedData)
        {
            if (receivedData.Contains("Nickname is already in use"))
            {
                this.server.username += "_";
                AttemptAuth();
                return;
            }

            if (receivedData.StartsWith("ERROR"))
            {
                if (!IsReconnecting)
                {
                    ReadOrWriteFailed = true;

                    var autoReconnect = Config.GetBoolean(Config.AutoReconnect);

                    var msg = autoReconnect
                        ? "Attempting to reconnect..."
                        : "Please try again later.";

                    AddError("Error with connection: \n" + msg);

                    DisconnectAsync(attemptReconnect: autoReconnect);
                }
                return;
            }

            if (receivedData.StartsWith("PING"))
            {
                await WriteLine(writer, receivedData.Replace("PING", "PONG"));
                return;
            }

            var parsedLine = new IrcMessage(receivedData);

            ReconnectionAttempts = 0;

            if (parsedLine.CommandMessage.Command == "CAP")
            {
                if (parsedLine.CommandMessage.Parameters[1] == "LS")
                {
                    var requirements = "";
                    var compatibleFeatues = parsedLine.TrailMessage.TrailingContent;
                    if (compatibleFeatues.Contains("znc.in/server-time-iso"))
                    {
                        requirements += "znc.in/server-time-iso ";
                    }

                    if (compatibleFeatues.Contains("multi-prefix"))
                    {
                        requirements += "multi-prefix ";
                    }

                    WriteLine("CAP REQ :" + requirements);
                    WriteLine("CAP END");
                }
            }
            else if (parsedLine.CommandMessage.Command == "JOIN")
            {
                var channel = parsedLine.TrailMessage.TrailingContent;
                if (parsedLine.PrefixMessage.Nickname == this.server.username)
                {
                    AddChannel(channel);
                }
                else
                {
                    if (parsedLine.CommandMessage.Parameters != null)
                    {
                        channel = parsedLine.CommandMessage.Parameters[0];
                    }
                    Message msg = new Message();
                    msg.Type = MessageType.Info;
                    msg.User = parsedLine.PrefixMessage.Nickname;

                    msg.Text = String.Format("({0}) {1}", parsedLine.PrefixMessage.Prefix, "joined the channel");
                    AddMessage(channel, msg);
                    channelStore[channel].AddUser(parsedLine.PrefixMessage.Nickname, true);
                }
            }
            else if (parsedLine.CommandMessage.Command == "PART")
            {
                var channel = parsedLine.TrailMessage.TrailingContent;
                if (parsedLine.PrefixMessage.Nickname == this.server.username)
                {
                    RemoveChannel(channel);
                }
                else
                {
                    if (parsedLine.CommandMessage.Parameters.Count > 0)
                    {
                        channel = parsedLine.CommandMessage.Parameters[0];
                    }
                    Message msg = new Message();
                    msg.Type = MessageType.Info;
                    msg.User = parsedLine.PrefixMessage.Nickname;

                    msg.Text = String.Format("({0}) {1}", parsedLine.PrefixMessage.Prefix, "left the channel");
                    AddMessage(channel, msg);
                    channelStore[channel].RemoveUser(parsedLine.PrefixMessage.Nickname);
                }
            }
            else if (parsedLine.CommandMessage.Command == "PRIVMSG")
            {
                // handle messages to this irc client
                var destination = parsedLine.CommandMessage.Parameters[0];
                var content = parsedLine.TrailMessage.TrailingContent;

                if (destination == server.username)
                {
                    destination = parsedLine.PrefixMessage.Nickname;
                }

                if (!channelList.Contains(destination))
                {
                    AddChannel(destination);
                }

                Message msg = new Message();

                msg.Type = MessageType.Normal;
                msg.User = parsedLine.PrefixMessage.Nickname;
                if (parsedLine.ServerTime != null)
                {
                    var time = DateTime.Parse(parsedLine.ServerTime);
                    msg.Timestamp = time.ToString("HH:mm");
                }

                if (content.Contains("ACTION"))
                {
                    msg.Text = content.Replace("ACTION ", "");
                    msg.Type = MessageType.Action;
                }
                else
                {
                    msg.Text = content;
                }

                if ((parsedLine.TrailMessage.TrailingContent.Contains(server.username) || parsedLine.CommandMessage.Parameters[0] == server.username))
                {
                    if (currentChannel != destination)
                    {
                        var toast = CreateMentionToast(parsedLine.PrefixMessage.Nickname, destination, content);
                        toast.ExpirationTime = DateTime.Now.AddDays(2);
                        ToastNotificationManager.CreateToastNotifier().Show(toast);
                        (App.Current as App).NumberPings++;
                    }

                    msg.Mention = true;
                }

                AddMessage(destination, msg);

            }
            else if (parsedLine.CommandMessage.Command == "KICK")
            {
                // handle messages to this irc client
                var destination = parsedLine.CommandMessage.Parameters[0];
                var reciever = parsedLine.CommandMessage.Parameters[1];
                var content = parsedLine.TrailMessage.TrailingContent;
                if (!channelList.Contains(destination))
                {
                    AddChannel(destination);
                }

                Message msg = new Message();

                msg.Type = MessageType.Info;

                if (reciever == server.username)
                {
                    var kickTitle = String.Format("{0} kicked you from {1}", parsedLine.PrefixMessage.Nickname, destination);

                    var toast = CreateBasicToast(kickTitle, content);
                    toast.ExpirationTime = DateTime.Now.AddDays(2);
                    ToastNotificationManager.CreateToastNotifier().Show(toast);
                    msg.User = parsedLine.PrefixMessage.Nickname;
                    msg.Text = "kicked you from the channel: " + content;
                }
                else
                {
                    msg.User = parsedLine.PrefixMessage.Nickname;
                    msg.Text = String.Format("kicked {0} from the channel: {1}", reciever, content);
                }

                AddMessage(destination, msg);
            }
            else if (parsedLine.CommandMessage.Command == "353")
            {
                // handle /NAMES
                var list = parsedLine.TrailMessage.TrailingContent.Split(' ').ToList();
                var channel = parsedLine.CommandMessage.Parameters[2];

                if (!channelList.Contains(channel))
                {
                    await AddChannel(channel);
                }

                channelStore[channel].AddUsers(list);
            }
            else if (parsedLine.CommandMessage.Command == "332")
            {
                // handle initial topic recieve
                var topic = parsedLine.TrailMessage.TrailingContent;
                var channel = parsedLine.CommandMessage.Parameters[1];

                if (!channelList.Contains(channel))
                {
                    await AddChannel(channel);
                }

                Message msg = new Message();
                msg.Type = MessageType.Info;

                msg.User = "";
                msg.Text = String.Format("Topic for channel {0}: {1}", channel, topic);
                AddMessage(channel, msg);
                channelStore[channel].SetTopic(topic);
            }
            else if (parsedLine.CommandMessage.Command == "TOPIC")
            {
                // handle topic recieved
                var topic = parsedLine.TrailMessage.TrailingContent;
                var channel = parsedLine.CommandMessage.Parameters[0];

                if (!channelList.Contains(channel))
                {
                    await AddChannel(channel);
                }

                Message msg = new Message();
                msg.Type = MessageType.Info;

                msg.User = "";
                msg.Text = String.Format("Topic for channel {0}: {1}", channel, topic);
                AddMessage(channel, msg);
                channelStore[channel].SetTopic(topic);
            }
            else if (parsedLine.CommandMessage.Command == "QUIT")
            {
                var username = parsedLine.PrefixMessage.Nickname;
                foreach (var channel in channelList)
                {
                    var users = channelStore[channel];
                    if (users.HasUser(username))
                    {
                        Message msg = new Message();
                        msg.Type = MessageType.Info;
                        msg.User = parsedLine.PrefixMessage.Nickname;
                        msg.Text = String.Format("({0}) {1}: {2}", parsedLine.PrefixMessage.Prefix, "quit the server", parsedLine.TrailMessage.TrailingContent);
                        AddMessage(channel, msg);
                        users.RemoveUser(username);
                    }
                }
            }
            else if (parsedLine.CommandMessage.Command == "MODE")
            {
                Debug.WriteLine(parsedLine.CommandMessage.Command + " - " + receivedData);

                if (parsedLine.CommandMessage.Parameters.Count > 2)
                {
                    var channel = parsedLine.CommandMessage.Parameters[0];

                    if (parsedLine.CommandMessage.Parameters.Count == 3)
                    {
                        string currentPrefix = channelStore[channel].GetPrefix(parsedLine.CommandMessage.Parameters[2]);
                        string prefix = "";
                        string mode = parsedLine.CommandMessage.Parameters[1];
                        if (mode == "+o")
                        {
                            if (currentPrefix.Length > 0 && currentPrefix[0] == '+')
                            {
                                prefix = "@+";
                            }
                            else
                            {
                                prefix = "@";
                            }
                        }
                        else if (mode == "-o")
                        {
                            if (currentPrefix.Length > 0 && currentPrefix[1] == '+')
                            {
                                prefix = "+";
                            }
                        }
                        else if (mode == "+v")
                        {
                            if (currentPrefix.Length > 0 && currentPrefix[0] == '@')
                            {
                                prefix = "@+";
                            }
                            else
                            {
                                prefix = "+";
                            }
                        }
                        else if (mode == "-v")
                        {
                            if (currentPrefix.Length > 0 && currentPrefix[0] == '@')
                            {
                                prefix = "@";
                            }
                            else
                            {
                                prefix = "";
                            }
                        }

                        channelStore[channel].ChangePrefix(parsedLine.CommandMessage.Parameters[2], prefix);
                    }

                    ClientMessage(channel, "Mode change: " + String.Join(" ", parsedLine.CommandMessage.Parameters));
                }
            }
            else if (parsedLine.CommandMessage.Command == "470")
            {
                RemoveChannel(parsedLine.CommandMessage.Parameters[1]);
                AddChannel(parsedLine.CommandMessage.Parameters[2]);
            }
            else if (WhoisCmds.Any(str => str.Contains(parsedLine.CommandMessage.Command)))
            {
                var cmd = parsedLine.CommandMessage.Command;
                if (currentWhois == "")
                {
                    currentWhois += "Whois for " + parsedLine.CommandMessage.Parameters[1] + ": \r\n";
                }

                var whoisLine = "";

                if (cmd == "330")
                {
                    whoisLine += parsedLine.CommandMessage.Parameters[1] + " " + parsedLine.TrailMessage.TrailingContent + " " + parsedLine.CommandMessage.Parameters[2] + " ";
                    currentWhois += whoisLine + "\r\n";

                }
                else
                {
                    for (int i = 2; i < parsedLine.CommandMessage.Parameters.Count; i++)
                    {
                        whoisLine += parsedLine.CommandMessage.Command + " " + parsedLine.CommandMessage.Parameters[i] + " ";
                    }
                    currentWhois += whoisLine + parsedLine.TrailMessage.TrailingContent + "\r\n";

                }

            }
            else if (parsedLine.CommandMessage.Command == "318")
            {
                Debug.WriteLine(currentWhois);
                Message msg = new Message();
                msg.Text = currentWhois;
                msg.Type = MessageType.Info;
                AddMessage(currentChannel, msg);

                currentWhois = "";
            }
            else if (parsedLine.CommandMessage.Command == "376")
            {
                if (server.nickservPassword != null && server.nickservPassword != "")
                {
                    SendMessage("nickserv", "identify " + server.nickservPassword);
                }

                if (server.channels != null && server.channels != "")
                {
                    var channelsList = server.channels.Split(',');
                    foreach (string channel in channelsList)
                    {
                        JoinChannel(channel);
                    }
                }
            }
            else
            {
                if (!parsedLine.PrefixMessage.IsUser)
                {
                    if (!channelList.Contains("Server"))
                    {
                        await AddChannel("Server");
                    }

                    Message msg = new Message();
                    msg.Text = parsedLine.OriginalMessage;
                    msg.Type = MessageType.Info;
                    msg.User = "";
                    AddMessage("Server", msg);
                }
                Debug.WriteLine(parsedLine.CommandMessage.Command + " - " + receivedData);
            }

            //Debug.WriteLine(parsedLine.CommandMessage.Command + " - " + receivedData);

        }


        public void SendAction(string message)
        {
            Message msg = new Message();

            msg.Text = message;
            msg.Type = MessageType.Action;
            msg.User = server.username;
            AddMessage(currentChannel, msg);

            WriteLine(String.Format("PRIVMSG {0} :\u0001ACTION {1}\u0001", currentChannel, message));
        }

        public void SendMessage(string channel, string message)
        {
            // todo
            Message msg = new Message();

            msg.Text = message;
            msg.User = server.username;
            msg.Type = MessageType.Normal;

            if (channelBuffers.Keys.Contains(channel))
                channelBuffers[channel].Add(msg);

            WriteLine(String.Format("PRIVMSG {0} :{1}", channel, message));
        }

        public string GetChannelTopic(string channel)
        {
            return channelStore[channel].Topic;
        }

        public void JoinChannel(string channel)
        {
            WriteLine(String.Format("JOIN {0}", channel));
        }

        public void PartChannel(string channel)
        {
            WriteLine(String.Format("PART {0}", channel));
            RemoveChannel(channel);
        }

        public void SendMessage(string message)
        {
            Message msg = new Message();

            msg.Text = message;
            msg.User = server.username;
            msg.Type = MessageType.Normal;

            AddMessage(currentChannel, msg);

            WriteLine(String.Format("PRIVMSG {0} :{1}", currentChannel, message));
        }

        public async void AddError(String message)
        {
            Message msg = new Message();

            msg.Text = message;
            msg.User = "Error";
            msg.Type = MessageType.Info;
            msg.Mention = true;

            AddMessage("Server", msg);
        }

        public async void AddMessage(string channel, Message msg)
        {
            if (!channelBuffers.ContainsKey(channel))
            {
                await AddChannel(channel);
            }

            channelBuffers[channel].Add(msg);
        }

        public async Task<bool> AddChannel(string channel)
        {
            if (channel == "")
            {
                return false;
            }

            if (!channelBuffers.Keys.Contains(channel) && !channelStore.Keys.Contains(channel) && !channelList.Contains(channel))
            {
                channelBuffers.Add(channel, new ObservableCollection<Message>());

                var comparer = Comparer<String>.Default;

                int i = 0;
                if (channelList.Count >= 1)
                {
                    i++;
                }

                while (i < channelList.Count && comparer.Compare(channelList[i].ToLower(), channel.ToLower()) < 0)
                    i++;

                channelList.Insert(i, channel);

                channelStore.Add(channel, new ChannelStore());
            }

            await Task.Delay(1);

            if (!Config.Contains(Config.SwitchOnJoin))
            {
                Config.SetBoolean(Config.SwitchOnJoin, true);
            }

            if (channelList.Contains(channel) && Config.GetBoolean(Config.SwitchOnJoin))
            {
                MainPage.instance.SwitchChannel(server.name, channel, true);
            }

            return channelList.Contains(channel);
        }

        public void RemoveChannel(string channel)
        {
            if (channelBuffers.Keys.Contains(channel))
            {
                channelBuffers.Remove(channel);
                channelList.Remove(channel);
                channelStore.Remove(channel);

                if (currentChannel == channel)
                {
                    currentChannel = "";
                }
            }
        }

        public void SwitchChannel(string channel)
        {
            if (channelBuffers.Keys.Contains(channel))
            {
                currentChannel = channel;
                ircMessages = channelBuffers[channel];
            }
        }

        public void ClientMessage(string channel, string text)
        {
            Message msg = new Message();
            msg.User = "";
            msg.Type = MessageType.Info;
            msg.Text = text;

            this.AddMessage(channel, msg);
        }

        public void ClientMessage(string text)
        {
            Message msg = new Message();
            msg.User = "";
            msg.Type = MessageType.Info;
            msg.Text = text;

            this.AddMessage(currentChannel, msg);
        }

        public async void WriteLine(string str)
        {
            await WriteLine(writer, str);
        }

        public async Task WriteLine(DataWriter writer, string str)
        {
            if (ReadOrWriteFailed)
                return;

            try
            {
                if (ConnCheck.HasInternetAccess && !IsReconnecting)
                {
                    writer.WriteString(str + "\r\n");
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }
            }
            catch (Exception e)
            {
                ReadOrWriteFailed = true;
                var autoReconnect = Config.GetBoolean(Config.AutoReconnect);

                var msg = autoReconnect
                    ? "Attempting to reconnect..."
                    : "Please try again later.";

                AddError("Error whilst connecting: " + e.Message + "\n" + msg);
                AddError(e.StackTrace);

                DisconnectAsync(attemptReconnect: autoReconnect);

                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        public static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public ObservableCollection<string> GetChannelUsers(string channel)
        {
            return channelStore[channel].SortedUsers;
        }

        public ObservableCollection<string> GetRawUsers(string channel)
        {
            return channelStore[channel].RawUsers;
        }

        public static ToastNotification CreateBasicToast(string title, string msg)
        {
            try
            {
                ToastVisual visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText { Text = title },
                            new AdaptiveText { Text = msg }
                        },
                        Attribution = new ToastGenericAttributionText
                        {
                            Text = "Information"
                        },
                    }
                };

                // Now we can construct the final toast content
                ToastContent toastContent = new ToastContent()
                {
                    Visual = visual,
                };

                // And create the toast notification
                return new ToastNotification(toastContent.GetXml());
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }

            return null;
        }

        public ToastNotification CreateMentionToast(string username, string channel, string text)
        {
            try
            {
                AdaptiveText message;
                if (text.StartsWith("ACTION"))
                {
                    message = new AdaptiveText
                    {
                        Text = text.Replace("ACTION", " * " + username)
                    };
                }
                else
                {
                    message = new AdaptiveText
                    {
                        Text = text
                    };
                }
                // Construct the visuals of the toast
                ToastVisual visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText {Text = "Message from " + username },
                            message
                        },
                        Attribution = new ToastGenericAttributionText
                        {
                            Text = "Mention in " + channel
                        },
                    }
                };

                ToastActionsCustom actions = new ToastActionsCustom()
                {
                    Inputs =
                    {
                        new ToastTextBox("tbReply")
                        {
                            PlaceholderContent = "Reply to this mention..."
                        }
                    },
                    Buttons =
                    {
                        new ToastButton("Reply", new QueryString()
                        {
                            { "action", "reply" },
                            { "server", server.name },
                            { "channel", channel },
                            { "username", username }

                        }.ToString())
                        {
                            ActivationType = ToastActivationType.Foreground,
                            ImageUri = "Assets/Send.png",
                            TextBoxId = "tbReply"
                        },
                    },
                };

                // Now we can construct the final toast content
                ToastContent toastContent = new ToastContent()
                {
                    Visual = visual,
                    Actions = actions, 

                    // Arguments when the user taps body of toast
                    Launch = new QueryString()
                    {
                        { "action", "viewConversation" },
                        { "server", server.name },
                        { "channel",  channel }

                    }.ToString()
                };

                // And create the toast notification
                return new ToastNotification(toastContent.GetXml());
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);

            }

            return null;
        }
    }
}
