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
        public string messageText { get; set; }
        public string messageColour { get; set; }
        public FontStyle messageFormat { get; set; }
    }
}
