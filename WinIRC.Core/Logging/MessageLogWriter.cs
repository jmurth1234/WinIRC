using System.Collections.Concurrent;
using IrcClientCore;
using WinIRC.Core.Persistence;

namespace WinIRC.Core.Logging;

public sealed class MessageLogWriter : IDisposable
{
    private readonly string rootDirectory;
    private readonly string server;
    private readonly ConcurrentQueue<LogEntry> pending = new();
    private readonly SemaphoreSlim signal = new(0);
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task worker;

    public MessageLogWriter(string server, string? rootDirectory = null)
    {
        this.server = server;
        this.rootDirectory = string.IsNullOrWhiteSpace(rootDirectory)
            ? Path.Combine(AppDataPaths.GetAppDataDirectory(), "logs")
            : rootDirectory;
        worker = Task.Run(ProcessAsync);
    }

    public string RootDirectory => rootDirectory;

    public void Add(string channel, Message message)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            channel = "Server";
        }

        pending.Enqueue(new LogEntry(channel, message));
        signal.Release();
    }

    public void Dispose()
    {
        cancellation.Cancel();
        signal.Release();

        try
        {
            worker.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Logging must never stop shutdown.
        }

        signal.Dispose();
        cancellation.Dispose();
    }

    private async Task ProcessAsync()
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                await signal.WaitAsync(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            while (pending.TryDequeue(out var entry))
            {
                try
                {
                    await WriteAsync(entry, cancellation.Token);
                }
                catch
                {
                    // Ignore log failures; chat handling should continue.
                }
            }
        }
    }

    private async Task WriteAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        var serverDirectory = Path.Combine(rootDirectory, SafePathSegment(server));
        Directory.CreateDirectory(serverDirectory);

        var fileName = $"{SafePathSegment(entry.Channel)}-{DateTime.Now:yyyy-MM-dd}.log";
        var path = Path.Combine(serverDirectory, fileName);
        await File.AppendAllTextAsync(path, MessageToString(entry.Message) + Environment.NewLine, cancellationToken);
    }

    private static string MessageToString(Message message)
    {
        var user = message.Type switch
        {
            MessageType.Action or MessageType.Info => "* " + message.User,
            _ when string.IsNullOrWhiteSpace(message.User) => "*",
            _ => "<" + message.User + ">"
        };

        var timestamp = string.IsNullOrWhiteSpace(message.Timestamp)
            ? message.Date.ToString("HH:mm")
            : message.Timestamp;
        return $"[{timestamp}] {user} {message.Text}";
    }

    private static string SafePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        sanitized = sanitized.Trim().Trim('.');
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    private sealed record LogEntry(string Channel, Message Message);
}
