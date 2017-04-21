using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;

namespace WinIRC.Net
{
    public class Message
    {
        public Message()
        {
            DateTime date = DateTime.Now;
            Timestamp = date.ToString("HH:mm");
        }

        public string Timestamp {
            get
            {
                if (_date == null)
                {
                    DateTime date = DateTime.Now;
                    _date = date.ToString("HH:mm");
                }
                return _date;
            }
            set
            {
                this._date = value;
            }
        }

        private string _date;

        public string User {
            get
            {
                if (Type == MessageType.Action || Type == MessageType.Info)
                {
                    return String.Format("* {0}", _username);
                }
                else if (_username == "" )
                {
                    return "*";
                }
                else
                { 
                    return String.Format("<{0}>", _username);
                }
            }
            set
            {
                this._username = value;
            }
        }

        private string _username;

        public string Text { get; set; }
        public bool Mention { get; internal set; }
        public MessageType Type { get; set; }
    }

    public enum MessageType
    {
        Normal, Action, Info
    }
}
