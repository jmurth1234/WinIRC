using System.Net.WebSockets;
using System.Text;
using IrcClientCore;
using WinIRC.Core.Irc;
using WinIRC.Core.Models;

namespace WinIRC.Core.Networking;

public sealed class WebSocketIrcConnection : ISocket
{
    private readonly CoreIrcClient parent;
    private readonly WinIrcServer server;
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private readonly StringBuilder receiveBuffer = new();
    private ClientWebSocket? webSocket;
    private CancellationTokenSource? connectionCts;
    private bool intentionalDisconnect;
    private bool readyRaised;
    private DateTimeOffset connectedAt;
    private int messagesReceived;
    private bool sawServerError;

    public WebSocketIrcConnection(CoreIrcClient parent, WinIrcServer server)
    {
        this.parent = parent;
        this.server = server;
    }

    public async Task Connect()
    {
        intentionalDisconnect = false;
        readyRaised = false;
        messagesReceived = 0;
        sawServerError = false;
        connectionCts = new CancellationTokenSource();
        webSocket = new ClientWebSocket();

        if (server.IgnoreCertErrors)
        {
            webSocket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        }

        try
        {
            parent.IsConnecting = true;
            var scheme = server.Ssl ? "wss" : "ws";
            var uri = new Uri($"{scheme}://{server.Hostname}:{server.Port}/");
            await webSocket.ConnectAsync(uri, connectionCts.Token);

            await parent.Dispatcher.InvokeAsync(() =>
            {
                parent.IsConnected = true;
                parent.IsConnecting = false;
                connectedAt = DateTimeOffset.UtcNow;
                parent.AddClientMessage("Server", $"Connected to websocket {server.Hostname}:{server.Port}.");
            });
            parent.NotifyStateChanged();
            _ = Task.Run(() => ReceiveLoopAsync(connectionCts.Token), CancellationToken.None);
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(3), connectionCts.Token).ConfigureAwait(false);
                RaiseReady();
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await parent.Dispatcher.InvokeAsync(() =>
            {
                parent.IsConnected = false;
                parent.IsConnecting = false;
                parent.AddError($"Could not connect to websocket: {ex.Message}");
            });
            parent.NotifyStateChanged();
            parent.HandleDisconnect?.Invoke(parent);
        }
    }

    public async Task WriteLine(string line)
    {
        if (webSocket == null || webSocket.State != WebSocketState.Open)
        {
            return;
        }

        await writeLock.WaitAsync();
        try
        {
            var bytes = Encoding.UTF8.GetBytes(line + "\n");
            await webSocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task Disconnect(string msg = "Powered by WinIRC", bool attemptReconnect = false)
    {
        intentionalDisconnect = true;
        connectionCts?.Cancel();

        if (parent.IsConnected)
        {
            await WriteLine($"QUIT :{msg}");
        }

        if (webSocket?.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "WinIRC disconnect", CancellationToken.None);
        }

        parent.IsConnected = false;
        parent.IsConnecting = false;
        webSocket?.Dispose();
    }

    public void Dispose()
    {
        intentionalDisconnect = true;
        connectionCts?.Cancel();
        webSocket?.Dispose();
        connectionCts?.Dispose();
        writeLock.Dispose();
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        Exception? disconnectException = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested && webSocket?.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messagesReceived++;
                sawServerError |= text.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase);
                if (string.Equals(text.Trim(), "SOCKET_CONNECTED_IRC", StringComparison.OrdinalIgnoreCase))
                {
                    RaiseReady();
                    continue;
                }

                receiveBuffer.Append(text);
                if (!result.EndOfMessage)
                {
                    continue;
                }

                FlushCompleteMessages();
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
                await parent.Dispatcher.InvokeAsync(() =>
                {
                    parent.ReadOrWriteFailed = disconnectException != null;
                    parent.IsConnected = false;

                    if (disconnectException != null)
                    {
                        parent.AddError($"Websocket error: {disconnectException.Message}");
                    }
                    else if (sawServerError)
                    {
                        parent.AddError("Websocket closed after reporting an IRC error. Automatic reconnect has been paused.");
                    }
                    else if (messagesReceived == 0 || lifetime < TimeSpan.FromSeconds(10))
                    {
                        parent.AddError("Websocket closed during login. Check the forwarder, TLS setting, username, and password.");
                    }
                    else
                    {
                        parent.AddError("Websocket connection closed.");
                    }
                });

                parent.NotifyStateChanged();
                parent.HandleDisconnect?.Invoke(parent);
            }
        }
    }

    private void FlushCompleteMessages()
    {
        var text = receiveBuffer.ToString();
        receiveBuffer.Clear();

        var lines = text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0 && !string.IsNullOrWhiteSpace(text))
        {
            _ = parent.Dispatcher.InvokeAsync(() => parent.RecieveLine(text.TrimEnd('\r', '\n')));
            return;
        }

        foreach (var line in lines)
        {
            _ = parent.Dispatcher.InvokeAsync(() => parent.RecieveLine(line.TrimEnd('\r')));
        }
    }

    private void RaiseReady()
    {
        if (readyRaised)
        {
            return;
        }

        readyRaised = true;
        _ = parent.Dispatcher.InvokeAsync(parent.AttemptAuth);
    }
}
