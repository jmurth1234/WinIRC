using IrcClientCore;
using WinIRC.Core.Dispatching;
using WinIRC.Core.Models;
using WinIRC.Core.Networking;

namespace WinIRC.Core.Irc;

public sealed class CoreIrcClient : IrcClientCore.Irc
{
    private readonly IIrcConnectionFactory connectionFactory;

    public CoreIrcClient(WinIrcServer server, IIrcConnectionFactory connectionFactory, IAppDispatcher dispatcher) : base(server)
    {
        this.connectionFactory = connectionFactory;
        Dispatcher = dispatcher;
        DebugMode = false;
    }

    public event EventHandler? StateChanged;

    public IAppDispatcher Dispatcher { get; }
    public new WinIrcServer Server => (WinIrcServer)base.Server;

    public override ISocket CreateConnection()
    {
        return connectionFactory.Create(this, Server);
    }

    public override IBuffer CreateChannelBuffer(string channel)
    {
        return new CrossPlatformBuffer();
    }

    public override async Task ProcessLine(IrcMessage parsedLine)
    {
        await base.ProcessLine(parsedLine);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddClientMessage(string channel, string text)
    {
        if (channel == IrcSession.ServerBufferName && ChannelList?.ServerLog != null)
        {
            ChannelList.ServerLog.ClientMessage(text);
            StateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (ChannelList != null && ChannelList.Contains(channel))
        {
            ChannelList[channel].ClientMessage(text);
            StateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        ClientMessage(channel, text);
    }
}
