using IrcClientCore;
using IrcClientCore.Handlers.BuiltIn;
using WinIRC.Core.Configuration;
using WinIRC.Core.Irc;
using WinIRC.Core.Models;
using WinIRC.Core.Networking;

namespace WinIRC.Tests.Core;

public sealed class IrcClientCoreSessionTests
{
    [Fact]
    public async Task ConnectUsesIrcClientCoreAuthenticationFlow()
    {
        var fixture = CreateFixture(new WinIrcServer
        {
            Name = "Example",
            Hostname = "irc.example.net",
            Port = 6667,
            Username = "tester",
            Password = "server-pass"
        });

        await fixture.Session.ConnectAsync();

        Assert.Contains("PASS server-pass", fixture.Socket.Writes);
        Assert.Contains("NICK tester", fixture.Socket.Writes);
        Assert.Contains("USER tester 8 * :tester", fixture.Socket.Writes);
    }

    [Fact]
    public async Task PingIsHandledByIrcClientCore()
    {
        var fixture = CreateFixture();

        await fixture.Session.ConnectAsync();
        await fixture.Session.Client.RecieveLine("PING :irc.example.net");

        Assert.Contains("PONG :irc.example.net", fixture.Socket.Writes);
    }

    [Fact]
    public async Task CommandManagerHandlesNormalMessages()
    {
        var fixture = CreateFixture();

        await fixture.Session.ConnectAsync();
        await fixture.Session.SendAsync("#winirc", "hello linux");

        Assert.Contains("PRIVMSG #winirc :hello linux", fixture.Socket.Writes);
    }

    [Fact]
    public async Task MessageEventsCanReadCurrentBufferWithoutReentrancy()
    {
        var fixture = CreateFixture();
        var snapshots = new List<int>();
        fixture.Session.SessionEvent += (_, args) =>
        {
            if (args.Kind == WinIRC.Core.Events.IrcSessionEventKind.MessageAdded)
            {
                snapshots.Add(fixture.Session.GetMessages(args.Channel ?? IrcSession.ServerBufferName).Count);
            }
        };

        await fixture.Session.ConnectAsync();
        fixture.Session.Client.AddClientMessage("Server", "one");
        fixture.Session.Client.AddClientMessage("Server", "two");

        Assert.Contains(1, snapshots);
        Assert.Contains(2, snapshots);
    }

    [Fact]
    public async Task ConnectedDiagnosticAppearsInServerBuffer()
    {
        var fixture = CreateFixture();

        await fixture.Session.ConnectAsync();

        Assert.Contains(fixture.Session.GetMessages(IrcSession.ServerBufferName), message =>
            message.Text.Contains("Connected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task NamesHandlerUpdatesUsersThroughIrcClientCore()
    {
        var fixture = CreateFixture();

        await fixture.Session.ConnectAsync();
        await fixture.Session.Client.AddChannel("#winirc");
        await fixture.Session.Client.RecieveLine(":irc.example.net 353 tester = #winirc :@alice +bob tester");
        await fixture.Session.Client.RecieveLine(":irc.example.net 366 tester #winirc :End of names");

        Assert.Equal(new[] { "alice", "bob", "tester" }, fixture.Session.GetUsers("#winirc"));
    }

    [Fact]
    public async Task ChannelListCallbackRaisesSortedEntries()
    {
        var fixture = CreateFixture();
        IReadOnlyList<ChannelListEntry>? entries = null;
        fixture.Session.SessionEvent += (_, args) =>
        {
            if (args.Kind == WinIRC.Core.Events.IrcSessionEventKind.ChannelListReceived)
            {
                entries = args.ChannelList;
            }
        };

        await fixture.Session.ConnectAsync();
        fixture.Session.Client.HandleDisplayChannelList?.Invoke(new List<ChannelListItem>
        {
            new() { Channel = "#small", Topic = "quiet", Users = 1 },
            new() { Channel = "#large", Topic = "busy", Users = 50 }
        });

        Assert.NotNull(entries);
        Assert.Equal(new[] { "#large", "#small" }, entries.Select(entry => entry.Channel));
    }

    [Fact]
    public async Task TabCompletionIncludesCommandsAndUsers()
    {
        var fixture = CreateFixture();

        await fixture.Session.ConnectAsync();
        await fixture.Session.Client.AddChannel("#winirc");
        await fixture.Session.Client.RecieveLine(":irc.example.net 353 tester = #winirc :@alice +bob tester");
        await fixture.Session.Client.RecieveLine(":irc.example.net 366 tester #winirc :End of names");

        Assert.Contains("/join", fixture.Session.GetTabCompletions(IrcSession.ServerBufferName, "/jo", 3));
        Assert.Equal(new[] { "alice" }, fixture.Session.GetTabCompletions("#winirc", "al", 2));
    }

    private static Fixture CreateFixture(WinIrcServer? server = null)
    {
        var socket = new FakeSocket();
        var factory = new FakeConnectionFactory(socket);
        var settings = new MemorySettingsStore();
        settings.SetBoolean(ConfigKeys.AutoReconnect, false);

        var session = new IrcSession(server ?? new WinIrcServer
        {
            Name = "Example",
            Hostname = "irc.example.net",
            Username = "tester"
        }, factory, settings);

        return new Fixture(session, socket);
    }

    private sealed record Fixture(IrcSession Session, FakeSocket Socket);

    private sealed class FakeConnectionFactory : IIrcConnectionFactory
    {
        private readonly FakeSocket socket;

        public FakeConnectionFactory(FakeSocket socket)
        {
            this.socket = socket;
        }

        public ISocket Create(CoreIrcClient parent, WinIrcServer server)
        {
            socket.Parent = parent;
            return socket;
        }
    }

    private sealed class FakeSocket : ISocket
    {
        public CoreIrcClient? Parent { get; set; }
        public List<string> Writes { get; } = new();

        public Task Connect()
        {
            if (Parent != null)
            {
                Parent.IsConnected = true;
                Parent.IsConnecting = false;
                Parent.AddClientMessage("Server", "Connected to fake server.");
                Parent.AttemptAuth();
            }

            return Task.CompletedTask;
        }

        public Task Disconnect(string msg = "Powered by WinIRC", bool attemptReconnect = false)
        {
            if (Parent != null)
            {
                Parent.IsConnected = false;
                Parent.IsConnecting = false;
            }

            return Task.CompletedTask;
        }

        public Task WriteLine(string line)
        {
            Writes.Add(line);
            return Task.CompletedTask;
        }
    }

    private sealed class MemorySettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, object> values = new(StringComparer.OrdinalIgnoreCase);

        public bool Contains(string key) => values.ContainsKey(key);
        public void SetString(string key, string value) => values[key] = value;
        public void SetInt(string key, int value) => values[key] = value;
        public void SetBoolean(string key, bool value) => values[key] = value;
        public bool GetBoolean(string key, bool defaultValue = false) => values.TryGetValue(key, out var value) && value is bool boolean ? boolean : defaultValue;
        public string GetString(string key, string defaultValue = "") => values.TryGetValue(key, out var value) && value is string text ? text : defaultValue;
        public int GetInt(string key, int defaultValue = 0) => values.TryGetValue(key, out var value) && value is int integer ? integer : defaultValue;
        public void RemoveKey(string key) => values.Remove(key);
    }
}
