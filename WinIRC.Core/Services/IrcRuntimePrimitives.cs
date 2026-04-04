using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IrcClientCore;

namespace WinIRC.Core.Services;

internal sealed class RuntimeServerDefinition : IrcServer
{
    public bool UseWebSocket { get; set; }
}

internal sealed class RuntimeIrcClient : Irc
{
    public RuntimeIrcClient(RuntimeServerDefinition server) : base(server)
    {
        DebugMode = true;
    }

    public override ISocket CreateConnection()
    {
        return Server is RuntimeServerDefinition runtimeServer && runtimeServer.UseWebSocket
            ? new RuntimeWebSocket(this)
            : new RuntimeSocket(this);
    }

    public override IBuffer CreateChannelBuffer(string channel)
    {
        return new RuntimeBuffer();
    }
}

internal sealed class RuntimeBuffer : IrcClientCore.Buffer
{
    public RuntimeBuffer()
    {
        Collection = new ObservableCollection<Message>();
    }
}

internal abstract class RuntimeSocketBase : ISocket, IDisposable
{
    private int _disconnecting;

    protected RuntimeSocketBase(RuntimeIrcClient parent)
    {
        Parent = parent;
    }

    protected RuntimeIrcClient Parent { get; }

    protected IrcServer Server => Parent.Server;

    public abstract Task Connect();

    public abstract Task WriteLine(string line);

    public async Task Disconnect(string msg = "Powered by WinIRC", bool attemptReconnect = false)
    {
        if (Interlocked.Exchange(ref _disconnecting, 1) != 0)
        {
            return;
        }

        try
        {
            if (Parent.IsConnected)
            {
                await WriteLine("QUIT :" + msg).ConfigureAwait(false);
            }
        }
        catch
        {
        }

        Parent.IsConnected = false;
        Parent.IsConnecting = false;

        await CloseAsync().ConfigureAwait(false);
        Parent.HandleDisconnect?.Invoke(Parent);
    }

    public void Dispose()
    {
        CloseAsync().GetAwaiter().GetResult();
    }

    protected void MarkConnected()
    {
        Parent.IsAuthed = false;
        Parent.ReadOrWriteFailed = false;
        Parent.IsConnected = true;
        Parent.IsConnecting = false;
        Parent.AttemptAuth();
    }

    protected async Task HandleLineAsync(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || string.Equals(line, "SOCKET_CONNECTED_IRC", StringComparison.Ordinal))
        {
            return;
        }

        await Parent.RecieveLine(line).ConfigureAwait(false);
    }

    protected async Task HandleSocketFailureAsync(string context, Exception exception)
    {
        Parent.AddError($"{context}: {exception.Message}");
        Parent.ReadOrWriteFailed = true;
        Parent.IsConnected = false;
        await Disconnect(attemptReconnect: false).ConfigureAwait(false);
    }

    protected abstract Task CloseAsync();
}

internal sealed class RuntimeSocket : RuntimeSocketBase
{
    private TcpClient? _tcpClient;
    private Stream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cancellation;

    public RuntimeSocket(RuntimeIrcClient parent) : base(parent)
    {
    }

    public override async Task Connect()
    {
        _cancellation = new CancellationTokenSource();

        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(Server.Hostname, Server.Port).ConfigureAwait(false);

            _stream = _tcpClient.GetStream();
            if (Server.Ssl)
            {
                var sslStream = new SslStream(
                    _stream,
                    false,
                    (_, _, _, errors) => errors == SslPolicyErrors.None);
                await sslStream.AuthenticateAsClientAsync(Server.Hostname).ConfigureAwait(false);
                _stream = sslStream;
            }

            _reader = new StreamReader(_stream, Encoding.UTF8);
            _writer = new StreamWriter(_stream, new UTF8Encoding(false))
            {
                AutoFlush = true,
                NewLine = "\r\n"
            };

            MarkConnected();
            _ = Task.Run(ReadLoopAsync);
        }
        catch (Exception exception)
        {
            await HandleSocketFailureAsync("Error whilst connecting", exception).ConfigureAwait(false);
        }
    }

    public override Task WriteLine(string line)
    {
        return _writer == null
            ? Task.CompletedTask
            : _writer.WriteLineAsync(line);
    }

    protected override Task CloseAsync()
    {
        _cancellation?.Cancel();
        _reader?.Dispose();
        _writer?.Dispose();
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _reader = null;
        _writer = null;
        _stream = null;
        _tcpClient = null;
        return Task.CompletedTask;
    }

    private async Task ReadLoopAsync()
    {
        if (_reader == null)
        {
            return;
        }

        try
        {
            while (_cancellation?.IsCancellationRequested == false)
            {
                var line = await _reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    break;
                }

                await HandleLineAsync(line).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            if (_cancellation?.IsCancellationRequested == true)
            {
                return;
            }

            await HandleSocketFailureAsync("Error with connection", exception).ConfigureAwait(false);
        }
    }
}

internal sealed class RuntimeWebSocket : RuntimeSocketBase
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cancellation;

    public RuntimeWebSocket(RuntimeIrcClient parent) : base(parent)
    {
    }

    public override async Task Connect()
    {
        _cancellation = new CancellationTokenSource();
        _socket = new ClientWebSocket();

        try
        {
            var uri = new Uri((Server.Ssl ? "wss" : "ws") + "://" + Server.Hostname + ":" + Server.Port + "/", UriKind.Absolute);
            await _socket.ConnectAsync(uri, _cancellation.Token).ConfigureAwait(false);
            MarkConnected();
            _ = Task.Run(ReadLoopAsync);
        }
        catch (Exception exception)
        {
            await HandleSocketFailureAsync("Error whilst connecting", exception).ConfigureAwait(false);
        }
    }

    public override Task WriteLine(string line)
    {
        if (_socket == null || _socket.State != WebSocketState.Open)
        {
            return Task.CompletedTask;
        }

        var payload = Encoding.UTF8.GetBytes(line + "\n");
        return _socket.SendAsync(
            new ArraySegment<byte>(payload),
            WebSocketMessageType.Text,
            true,
            _cancellation?.Token ?? CancellationToken.None);
    }

    protected override async Task CloseAsync()
    {
        if (_cancellation != null)
        {
            _cancellation.Cancel();
        }

        if (_socket != null)
        {
            try
            {
                if (_socket.State == WebSocketState.Open)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch
            {
            }

            _socket.Dispose();
            _socket = null;
        }
    }

    private async Task ReadLoopAsync()
    {
        if (_socket == null)
        {
            return;
        }

        var buffer = new byte[4096];
        using var builder = new MemoryStream();

        try
        {
            while (_cancellation?.IsCancellationRequested == false && _socket.State == WebSocketState.Open)
            {
                builder.SetLength(0);
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellation?.Token ?? CancellationToken.None).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await Disconnect(attemptReconnect: false).ConfigureAwait(false);
                        return;
                    }

                    builder.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var payload = Encoding.UTF8.GetString(builder.ToArray());
                var lines = payload.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    await HandleLineAsync(line).ConfigureAwait(false);
                }
            }
        }
        catch (Exception exception)
        {
            if (_cancellation?.IsCancellationRequested == true)
            {
                return;
            }

            await HandleSocketFailureAsync("Error with websocket connection", exception).ConfigureAwait(false);
        }
    }
}
