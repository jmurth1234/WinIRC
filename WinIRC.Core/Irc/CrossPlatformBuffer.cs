using IrcClientCore;

namespace WinIRC.Core.Irc;

public sealed class CrossPlatformBuffer : IrcClientCore.Buffer
{
    public CrossPlatformBuffer(int maxMessages = 1000)
    {
        Collection = new BoundedObservableCollection<Message>(maxMessages);
    }
}
