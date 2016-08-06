using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIRC.Net
{
    public class IrcServer
    {
        public bool invalid { get; set; }

        public string name { get; set; }
        public string hostname { get; set; }
        public int port { get; set; }
        public bool ssl { get; set; }
        public bool webSocket { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string nickservPassword { get; set; }

        // channels are a string seperated by commas
        public string channels { get; set; }
    }
}
