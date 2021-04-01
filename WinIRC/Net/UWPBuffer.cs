using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinIRC.Utils;

namespace WinIRC.Net
{
    class UWPBuffer : IrcClientCore.Buffer
    {
        public UWPBuffer(string server, string channel)
        {
            this.Collection = new MessageCollection(1000, server, channel);
        }
    }
}
