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

        public string name { get; set; } = "";
        public string hostname { get; set; } = "";
        public int port { get; set; } = 6667;
        public bool ssl { get; set; } = false;
        public bool webSocket { get; set; } = false;
        public string username { get; set; } = "";
        public string password { get; set; } = "";
        public string nickservPassword { get; set; } = "";

        // channels are a string seperated by commas
        public string channels { get; set; } = "";

        public override String ToString()
        {
            return name;
        }
    }
}
