using WinIRC.Core.Models;

namespace WinIRC.Core.Networking;

public interface IIrcConnectionFactory
{
    IrcClientCore.ISocket Create(Core.Irc.CoreIrcClient parent, WinIrcServer server);
}
