using IrcClientCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using WinIRC.Net;

namespace WinIRC.Utils
{
    public class MessageCollection : ObservableCollection<Message>
    {
        public int MaxSize { get; }
        public MessageCollectionLogWriter LogWriter { get; }

        public string Server { get; private set; }
        public string Channel { get; private set; }

        public MessageCollection(int size, string server, string channel) : base()
        {
            this.MaxSize = size;
            if (Config.GetBoolean(Config.EnableLogs))
            {
                this.LogWriter = new MessageCollectionLogWriter(this, server, channel);
                Task.Run(this.LogWriter.Process);
            }

            this.CollectionChanged += MessageCollection_CollectionChanged;
        }

        private void MessageCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.Count > MaxSize)
            {
                RemoveAt(0);
            }


            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var items = e.NewItems;

                if (LogWriter != null && !LogWriter.Error)
                    foreach (var item in items) LogWriter.Add(item as Message);
            }
        }

    }

    public class MessageCollectionLogWriter
    {
        private ConcurrentQueue<Message> msgQueue;
        private MessageCollection msgs;
        private string server;
        private string channel;

        private StorageFolder serverFolder;
        private string currentDate;
        private StreamWriter sw;
        private bool PathChanged;

        public StorageFile LogFile { get; private set; }
        public bool Error { get; private set; }

        private readonly System.Threading.EventWaitHandle waitHandle = new System.Threading.AutoResetEvent(true);

        public MessageCollectionLogWriter(MessageCollection msgs, string server, string channel)
        {
            this.server = server;
            this.channel = channel;
            msgQueue = new ConcurrentQueue<Message>();
            this.msgs = msgs;
        }

        public async Task OpenFile()
        {
            if (sw != null) await sw.FlushAsync();
            LogFile = await serverFolder.CreateFileAsync(channel + "-" + currentDate + ".log", CreationCollisionOption.OpenIfExists);
            PathChanged = true;
            await WriteLine("----- Opened log on " + currentDate + " " + DateTime.Now.ToString("hh:mm") + " -----");
        }

        public void Add(Message message)
        {
            msgQueue.Enqueue(message);
            waitHandle.Set();
        }

        public async Task WriteLine(string str)
        {
            if (PathChanged || sw == null)
            {
                sw = File.AppendText(LogFile.Path);
            }

            await sw.WriteLineAsync(str);
            await sw.FlushAsync();
            PathChanged = false;
            //await FileIO.AppendTextAsync(LogFile, str + "\r\n");
        }

        private string MessageToString(Message msg)
        {
            var user = "";

            if (msg.Type == MessageType.Action || msg.Type == MessageType.Info)
            {
                user = String.Format("* {0}", msg.User);
            }
            else if (msg.User == "")
            {
                user = "*";
            }
            else
            {
                user = String.Format("<{0}>", msg.User);
            }

            return $"[{msg.Timestamp}] {user} {msg.Text}";
        }

        public async Task Process()
        {
            while (!Error)
            {
                if (msgQueue.Count == 0)
                {
                    waitHandle.WaitOne();
                }

                try
                {
                    var folderList = StorageApplicationPermissions.FutureAccessList;
                    if (LogFile == null && Config.GetBoolean(Config.EnableLogs) && folderList.ContainsItem(Config.LogsFolder))
                    {
                        var parent = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(Config.LogsFolder);
                        serverFolder = await parent.CreateFolderAsync(server, CreationCollisionOption.OpenIfExists);
                        currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                        await OpenFile();
                    }
                    else if (LogFile != null)
                    {
                        var newDate = DateTime.Now.ToString("yyyy-MM-dd");

                        if (newDate != currentDate)
                        {
                            currentDate = newDate;
                            await OpenFile();
                        }

                        Message msg;
                        msgQueue.TryDequeue(out msg);

                        if (msg != null)
                        {
                            await WriteLine(MessageToString(msg));
                        }
                    }
                    else
                    {
                        Error = true;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Error = true;
                    Message error = new Message()
                    {
                        User = "Error",
                        Type = MessageType.Info,
                        Text = e.Message + "\r\n Logging disabled for this channel"
                    };
                    msgs.Add(error);
                }

                waitHandle.Reset();
            }
        }
    }
}
