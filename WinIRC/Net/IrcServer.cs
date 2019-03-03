using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIRC.Net
{
    public class WinIrcServer : IrcClientCore.IrcServer
    {
        public bool invalid { get; set; }
        public bool webSocket { get; set; } = false;
    }
}
