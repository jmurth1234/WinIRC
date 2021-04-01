using IrcClientCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIRC.Ui
{
    public class MessageGrouper
    {
        public ObservableCollection<Message> Original { get; private set; }
        public ObservableCollection<MessageGroup> Grouped { get; } = new ObservableCollection<MessageGroup>();

        public MessageGrouper(ObservableCollection<Message> messages)
        {
            this.Original = messages;

            foreach (var message in messages)
            {
                AddMessage(message);
            }

            this.Original.CollectionChanged += Original_CollectionChanged;
        }

        private void Original_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                Grouped.Clear();
                return;
            }

            foreach (var message in e.NewItems)
            {
                AddMessage(message as Message);
            }
        }

        public void AddMessage(Message message)
        {
            if (!Config.GetBoolean(Config.ModernChat))
            {
                Grouped.Add(new MessageGroup()
                {
                    Parent = message
                });

                return;
            }

            try
            {
                var CurrentItem = Grouped.Last();

                if (CurrentItem.Parent.User == message.User && CurrentItem.Parent.Type == message.Type)
                {
                    CurrentItem.Children.Add(message);
                    return;
                }

                if (CurrentItem.Parent.Type == MessageType.JoinPart && message.Type == MessageType.JoinPart)
                {
                    CurrentItem.Children.Add(message);
                    return;
                }
            } catch { }

            var group = new MessageGroup()
            {
                Parent = message
            };

            group.Children.Add(message);

            Grouped.Add(group);
        }

    }

    public class MessageGroup
    {
        public Message Parent { get; set; }
        public ObservableCollection<Message> Children { get; set; } = new ObservableCollection<Message>();
    }
}
