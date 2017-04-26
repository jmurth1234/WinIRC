using System;

namespace WinIRC.Net
{
    internal class MessageEventArgs : EventArgs
    {
        public IrcMessage Message { get; set; }
        public Boolean Cancel { get; set; }

        public MessageEventArgs(IrcMessage parsedLine)
        {
            this.Message = parsedLine;
        }
    }
}