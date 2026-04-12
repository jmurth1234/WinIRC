using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using IrcClientCore;
using WinIRC.Core.Irc;
using WinIRC.Core.Models;

namespace WinIRC.Core.Networking;

public sealed class TcpIrcConnection : ISocket
{
    private readonly CoreIrcClient parent;
    private readonly WinIrcServer server;
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private CancellationTokenSource? connectionCts;
    private TcpClient? client;
    private Stream? stream;
    private StreamReader? reader;
    private StreamWriter? writer;
    private bool intentionalDisconnect;
    private DateTimeOffset connectedAt;
    private int linesReceived;
    private bool sawServerError;

    public TcpIrcConnection(CoreIrcClient parent, WinIrcServer server)
    {
        this.parent = parent;
        this.server = server;
    }

    public async Task Connect()
    {
        intentionalDisconnect = false;
        linesReceived = 0;
        sawServerError = false;
        connectionCts = new CancellationTokenSource();
        client = new TcpClient();

        try
        {
            await parent.Dispatcher.InvokeAsync(() =>
            {
                parent.IsConnecting = true;
                parent.ReadOrWriteFailed = false;
                parent.IsAuthed = false;
            });

            await client.ConnectAsync(server.Hostname, server.Port);
            stream = client.GetStream();
            if (server.Ssl)
            {
                var sslStream = new SslStream(stream, leaveInnerStreamOpen: false, (_, _, _, errors) => server.IgnoreCertErrors || errors == SslPolicyErrors.None);
                await sslStream.AuthenticateAsClientAsync(server.Hostname);
                stream = sslStream;
            }

            reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            writer = new StreamWriter(stream, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true)
            {
                NewLine = "\r\n",
                AutoFlush = true
            };

            await parent.Dispatcher.InvokeAsync(() =>
            {
                parent.IsConnected = true;
                parent.IsConnecting = false;
                connectedAt = DateTimeOffset.UtcNow;
                parent.AddClientMessage("Server", $"Connected to {server.Hostname}:{server.Port}.");
            });
            _ = Task.Run(() => ReadLoopAsync(connectionCts.Token), CancellationToken.None);
            await parent.Dispatcher.InvokeAsync(parent.AttemptAuth);
            parent.NotifyStateChanged();
        }
        catch (Exception ex)
        {
            await parent.Dispatcher.InvokeAsync(() =>
            {
                parent.IsConnected = false;
                parent.IsConnecting = false;
                parent.AddError($"Error whilst connecting: {ex.Message}");
            });
            parent.NotifyStateChanged();
            parent.HandleDisconnect?.Invoke(parent);
        }
    }

    public async Task WriteLine(string line)
    {
        if (writer == null)
        {
            return;
        }

        await writeLock.WaitAsync();
        try
        {
            await writer.WriteLineAsync(line);
            await writer.FlushAsync();
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task Disconnect(string msg = "Powered by WinIRC", bool attemptReconnect = false)
    {
        var shouldReconnect = attemptReconnect && server.ShouldReconnect;
        intentionalDisconnect = !shouldReconnect;

        if (writer != null && parent.IsConnected)
        {
            await WriteLine($"QUIT :{msg}");
        }

        parent.IsConnected = false;
        parent.IsConnecting = false;
        connectionCts?.Cancel();
        client?.Close();
        stream?.Close();

        if (shouldReconnect)
        {
            _ = Task.Run(async () =>
            {
                parent.ReconnectionAttempts++;
                await Task.Delay(parent.ReconnectionAttempts < 3 ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1));
                if (!intentionalDisconnect && server.ShouldReconnect)
                {
                    await Connect();
                }
            });
        }
        else
        {
            parent.HandleDisconnect?.Invoke(parent);
        }
    }

    public void Dispose()
    {
        intentionalDisconnect = true;
        connectionCts?.Cancel();
        reader?.Dispose();
        writer?.Dispose();
        stream?.Dispose();
        client?.Dispose();
        connectionCts?.Dispose();
        writeLock.Dispose();
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        Exception? disconnectException = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested && reader != null)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    break;
                }

                linesReceived++;
                sawServerError |= line.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase);
                await parent.Dispatcher.InvokeAsync(() => parent.RecieveLine(line));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            disconnectException = ex;
        }
        finally
        {
            if (!intentionalDisconnect)
            {
                var lifetime = DateTimeOffset.UtcNow - connectedAt;
                var reconnect = server.ShouldReconnect && !sawServerError && linesReceived > 0 && lifetime >= TimeSpan.FromSeconds(10);

                await parent.Dispatcher.InvokeAsync(() =>
                {
                    parent.ReadOrWriteFailed = disconnectException != null;
                    parent.IsConnected = false;

                    if (disconnectException != null)
                    {
                        parent.AddError($"Error with connection: {disconnectException.Message}");
                    }
                    else if (sawServerError)
                    {
                        parent.AddError("Server closed the connection after reporting an IRC error. Automatic reconnect has been paused.");
                    }
                    else if (linesReceived == 0 || lifetime < TimeSpan.FromSeconds(10))
                    {
                        parent.AddError("Server closed the connection during login. Check the hostname, port, TLS setting, username, and password.");
                    }
                    else
                    {
                        parent.AddError("Connection closed by remote host.");
                    }
                });

                parent.NotifyStateChanged();

                if (reconnect)
                {
                    await Disconnect(attemptReconnect: true);
                }
                else
                {
                    parent.HandleDisconnect?.Invoke(parent);
                }
            }
        }
    }
}
