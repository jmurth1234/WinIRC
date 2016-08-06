using Microsoft.QueryStringDotNET;
using NotificationsExtensions;
using NotificationsExtensions.Toasts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private string lightTextColor;
        private string chatTextColor;

        public Action<Irc> HandleDisconnect { get; set; }

        public Irc()
        {
            ircMessages = new ObservableCollection<Message>();
            channelList = new ObservableCollection<string>();
            channelBuffers = new Dictionary<string, ObservableCollection<Message>>();
            channelStore = new Dictionary<string, ChannelStore>();
            IsAuthed = false;

            lightTextColor = (bool) roamingSettings.Values["darktheme"] ? "#FFBBBBBB" : "#FF444444";
            chatTextColor = (bool) roamingSettings.Values["darktheme"] ? "#FFFFFFFF" : "#FF000000";
        }

        public virtual async void Connect() { }
        public virtual async void Disconnect(string msg = "Powered by WinIRC") { }

        internal void AttemptAuth()
        {

            // Auth to the server
            Debug.WriteLine("Attempting to auth");

            System.Threading.Timer timer = null;
            AttemptRegister();

            timer = new System.Threading.Timer((obj) =>
            {
                WriteLine("CAP LS");

                if (server.password != "")
                {
                    WriteLine("PASS " + server.password);
                }
                //WriteLine(String.Format("JOIN {0}", "#rymate"));
                timer.Dispose();
            }, null, 300, System.Threading.Timeout.Infinite);

            IsAuthed = true;
        }

        private async void AttemptRegister()
        {
            try {
                writer.WriteString(String.Format("NICK {0}", server.username) + "\r\n");
                writer.WriteString(String.Format("USER {0} {1} * :{2}", server.username, "8", server.username) + "\r\n");
                await writer.StoreAsync();
                await writer.FlushAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        internal void HandleLine(string receivedData)
        {
            if (receivedData.StartsWith("PING"))
            {
                WriteLine(receivedData.Replace("PING", "PONG"));
                return;
            }

            var parsedLine = new IrcMessage(receivedData);

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
                    msg.messageColour = lightTextColor;
                    msg.messageFormat = FontStyle.Italic;

                    msg.messageText = String.Format("* {0} ({1}) {2}", parsedLine.PrefixMessage.Nickname, parsedLine.PrefixMessage.Prefix, "joined the channel");
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
                    msg.messageColour = lightTextColor;
                    msg.messageFormat = FontStyle.Italic;

                    msg.messageText = String.Format("* {0} ({1}) {2}", parsedLine.PrefixMessage.Nickname, parsedLine.PrefixMessage.Prefix, "left the channel");
                    AddMessage(channel, msg);
                    channelStore[channel].RemoveUser(parsedLine.PrefixMessage.Nickname);
                }
            }
            else if (parsedLine.CommandMessage.Command == "PRIVMSG")
            {
                // handle messages to this irc client
                var destination = parsedLine.CommandMessage.Parameters[0];
                var content = parsedLine.TrailMessage.TrailingContent;
                if (!channelList.Contains(destination))
                {
                    AddChannel(destination);
                }

                Message msg = new Message();

                msg.messageColour = chatTextColor;

                if (parsedLine.TrailMessage.TrailingContent.Contains(server.username))
                {
                    var toast = CreateMentionToast(parsedLine.PrefixMessage.Nickname, destination, content);
                    toast.ExpirationTime = DateTime.Now.AddDays(2);
                    ToastNotificationManager.CreateToastNotifier().Show(toast);
                }

                if (content.Contains("ACTION"))
                {
                    content = content.Replace("ACTION ", "");
                    msg.messageText = String.Format(" * {0} {1}", parsedLine.PrefixMessage.Nickname, content);
                    msg.messageFormat = FontStyle.Italic;
                }
                else
                {
                    msg.messageText = String.Format("<{0}> {1}", parsedLine.PrefixMessage.Nickname, content);
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

                msg.messageColour = chatTextColor;

                if (reciever == server.username)
                {
                    var kickTitle = String.Format("{0} kicked you from {1}", parsedLine.PrefixMessage.Nickname, destination);

                    var toast = CreateBasicToast(kickTitle, content);
                    toast.ExpirationTime = DateTime.Now.AddDays(2);
                    ToastNotificationManager.CreateToastNotifier().Show(toast);
                    msg.messageText = String.Format("{0} kicked you from the channel: {1}", parsedLine.PrefixMessage.Nickname, content);
                }
                else
                {
                    msg.messageText = String.Format("{0} kicked {1} from the channel: {1}", parsedLine.PrefixMessage.Nickname, reciever, content);
                }

                AddMessage(destination, msg);
            }
            else if (parsedLine.CommandMessage.Command == "353")
            {
                // handle /NAMES
                var list = parsedLine.TrailMessage.TrailingContent.Split(' ').ToList();
                var channel = parsedLine.CommandMessage.Parameters[2];

                channelStore[channel].AddUsers(list);
            }
            else if (parsedLine.CommandMessage.Command == "332")
            {
                // handle /TOPIC
                var topic = parsedLine.TrailMessage.TrailingContent;
                var channel = parsedLine.CommandMessage.Parameters[1];
                Message msg = new Message();
                msg.messageColour = lightTextColor;
                msg.messageFormat = FontStyle.Italic;

                msg.messageText = String.Format("Topic for channel {0}: {1}", channel, topic);
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
                        msg.messageColour = lightTextColor;
                        msg.messageFormat = FontStyle.Italic;
                        msg.messageText = String.Format("* {0} ({1}) {2}: {3}", parsedLine.PrefixMessage.Nickname, parsedLine.PrefixMessage.Prefix, "quit the server", parsedLine.TrailMessage.TrailingContent);
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
                            if (currentPrefix[0] == '+')
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
                            if (currentPrefix[1] == '+')
                            {
                                prefix = "+";
                            }
                        }
                        else if (mode == "+v")
                        {
                            if (currentPrefix[0] == '@')
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
                            if (currentPrefix[0] == '@')
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
                msg.messageText = currentWhois;
                msg.messageColour = lightTextColor;
                AddMessage(currentChannel, msg);

                currentWhois = "";
            }
            else if (parsedLine.CommandMessage.Command == "376")
            {
                var channelsList = server.channels.Split(',');
                foreach (string channel in channelsList)
                {
                    JoinChannel(channel);
                }
            }
            else
            {
                if (!parsedLine.PrefixMessage.IsUser)
                {
                    if (!channelList.Contains("Server"))
                    {
                        AddChannel("Server");
                    }

                    Message msg = new Message();
                    msg.messageText = parsedLine.OriginalMessage;
                    msg.messageColour = lightTextColor;
                    AddMessage("Server", msg);
                }
                Debug.WriteLine(parsedLine.CommandMessage.Command + " - " + receivedData);
            }

            //Debug.WriteLine(parsedLine.CommandMessage.Command + " - " + receivedData);

        }

        public void SendAction(string message)
        {
            Message msg = new Message();

            msg.messageText = String.Format(" * {0} {1}", server.username, message);
            msg.messageColour = lightTextColor;
            msg.messageFormat = FontStyle.Italic;
            AddMessage(currentChannel, msg);

            WriteLine(String.Format("PRIVMSG {0} :\u0001ACTION {1}\u0001", currentChannel, message));
        }

        public void MentionReply(string channel, string message)
        {
            // todo
            Message msg = new Message();

            msg.messageText = String.Format("<{0}> {1}", server.username, message);
            msg.messageColour = lightTextColor;

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
            AddChannel(channel);
        }

        public void PartChannel(string channel)
        {
            WriteLine(String.Format("PART {0}", channel));
            RemoveChannel(channel);
        }

        public void SendMessage(string message)
        {
            Message msg = new Message();

            msg.messageText = String.Format("<{0}> {1}", server.username, message);
            msg.messageColour = lightTextColor;                               
            AddMessage(currentChannel, msg);

            WriteLine(String.Format("PRIVMSG {0} :{1}", currentChannel, message));
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
            msg.messageColour = (bool)Config.GetBoolean(Config.DarkTheme) ? "#FFBBBBBB" : "#FF444444";
            msg.messageFormat = FontStyle.Italic;
            msg.messageText = text;

            this.AddMessage(channel, msg);
        }

        public void ClientMessage(string text)
        {
            Message msg = new Message();
            msg.messageColour = (bool)Config.GetBoolean(Config.DarkTheme) ? "#FFBBBBBB" : "#FF444444";
            msg.messageFormat = FontStyle.Italic;
            msg.messageText = text;

            this.AddMessage(currentChannel, msg);
        }

        public async void WriteLine(string str)
        {
            try
            {
                writer.WriteString(str + "\r\n");
                await writer.StoreAsync();
                await writer.FlushAsync();
            }
            catch (Exception e)
            {
                MessageDialog dialog = new MessageDialog("An error occured: " + e.Message + ". Disconnecting");
                HandleDisconnect(this);
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
                // Construct the visuals of the toast
                ToastVisual visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText {Text = "Message from " + username },
                            new AdaptiveText {Text = text }
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
