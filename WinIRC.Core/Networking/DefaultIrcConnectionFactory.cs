using WinIRC.Core.Models;

namespace WinIRC.Core.Networking;

public sealed class DefaultIrcConnectionFactory : IIrcConnectionFactory
{
    public IrcClientCore.ISocket Create(Core.Irc.CoreIrcClient parent, WinIrcServer server)
    {
        return server.WebSocket
            ? new WebSocketIrcConnection(parent, server)
            : new TcpIrcConnection(parent, server);
    }
}
