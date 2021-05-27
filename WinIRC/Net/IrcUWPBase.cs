using IrcClientCore;
using IrcClientCore.Handlers.BuiltIn;
using Microsoft.AppCenter.Analytics;
using Microsoft.QueryStringDotNET;
using NotificationsExtensions;
using NotificationsExtensions.Toasts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Notifications;
using WinIRC.Utils;

namespace WinIRC.Net
{
    public class IrcUWPBase : Irc, IDisposable
    {
        Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

        public string buffer;
        public string currentChannel;

        private readonly int MAX_MESSAGES = 1000;

        public bool IsBouncer { get; private set; }
        public new Action<IrcUWPBase> HandleDisconnect { get; set; }
        public ObservableCollection<Message> ircMessages { get; set; }

        public IrcUWPBase(WinIrcServer server) : base(server)
        {
            DebugMode = false;
        }

        public new void Initialise()
        {
            base.Initialise();
            Mentions.CollectionChanged += Mentions_CollectionChanged;
            HandleDisplayChannelList = ShowChannels;
        }

        public override ISocket CreateConnection()
        {
            var server = (WinIrcServer)Server;
            if (server.webSocket)
            {
                return new IrcWebSocket(this);
            }
            return new IrcSocket(this);
        }

        public override async Task ProcessLine(IrcMessage parsedLine)
        {
            var hasHandlers = HandlerManager.HasHandler(parsedLine.CommandMessage.Command);

            await Task.Run(() =>
            {
                Analytics.TrackEvent(hasHandlers ? "Processing Irc Line" : "Processing Unhandled IRC line", new Dictionary<string, string> {
                    { "Command", parsedLine.CommandMessage.Command}
                });
            });

            await base.ProcessLine(parsedLine);
        }

        private void ShowChannels(List<ChannelListItem> obj)
        {
            var dialog = MainPage.instance.ShowChannelsList(obj);

            if (dialog == null) return;
            dialog.JoinChannelClick += Dialog_JoinChannelClick; 
        }

        private void Dialog_JoinChannelClick(ChannelListItem item)
        {
            JoinChannel(item.Channel);
            MainPage.instance.SwitchChannel(Server.Name, item.Channel, true);
        }

        private void Mentions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var message = e.NewItems[0] as Message;

            if (currentChannel != message.Channel || (App.Current as App).IncrementPings == true || MainPage.instance.currentServer == this.Server.Name)
            {
                var toast = CreateMentionToast(Server.Name, message.User, message.Channel, message.Text);
                toast.ExpirationTime = DateTime.Now.AddDays(2);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
                (App.Current as App).NumberPings++;
            }
        }

        public void SendMessage(string message)
        {
            this.CommandManager.HandleCommand(currentChannel, message);
        }

        public override IrcClientCore.IBuffer CreateChannelBuffer(string channel)
        {
            Debug.WriteLine("logging " + channel);
            return new UWPBuffer(Server.Name, channel);
        }

        public new async Task<bool> AddChannel(string channel)
        {
            await base.AddChannel(channel);

            if (!Config.Contains(Config.SwitchOnJoin))
            {
                Config.SetBoolean(Config.SwitchOnJoin, true);
            }

            if (ChannelList.Contains(channel) && Config.GetBoolean(Config.SwitchOnJoin) && !IsBouncer)
            {
                MainPage.instance.SwitchChannel(Server.Name, channel, true);
            }

            return ChannelList.Contains(channel);
        }

        public new void RemoveChannel(string channel)
        {
            base.RemoveChannel(channel);

            if (currentChannel == channel)
            {
                currentChannel = "";
            }
        }

        public void SwitchChannel(string channel)
        {
            if (channel == null)
            {
                return;
            }

            if (ChannelList.Contains(channel))
            {
                if (currentChannel != null) ChannelList[currentChannel].CurrentlyViewing = false;
                currentChannel = channel;
                ircMessages = (ChannelList[channel].Buffers as UWPBuffer).Collection;
                ChannelList[channel].CurrentlyViewing = true;
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

        public static ToastNotification CreateMentionToast(string server, string username, string channel, string text)
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
                            { "server", server },
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
                        { "server", server },
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

        public void Dispose()
        {
            try
            {
                (this.Connection as UWPSocketBase).Dispose();
                Server = null;

                HandleDisconnect = null;
            }
            catch (NullReferenceException e)
            {
                // catching silently, sorry
            }
        }
    }
}
