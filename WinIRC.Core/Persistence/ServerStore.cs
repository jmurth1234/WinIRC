using System.Collections.ObjectModel;
using System.Text.Json;
using System.Xml.Linq;
using WinIRC.Core.Models;

namespace WinIRC.Core.Persistence;

public sealed class ServerStore
{
    private readonly string filePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ServerStore(string appDataDirectory)
    {
        Directory.CreateDirectory(appDataDirectory);
        filePath = Path.Combine(appDataDirectory, "saved-servers.json");
    }

    public ObservableCollection<WinIrcServer> Servers { get; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Servers.Clear();

        if (!File.Exists(filePath))
        {
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var servers = await JsonSerializer.DeserializeAsync<List<WinIrcServer>>(stream, JsonOptions, cancellationToken)
            ?? new List<WinIrcServer>();

        foreach (var server in servers.OrderBy(server => server.Name, StringComparer.OrdinalIgnoreCase))
        {
            Servers.Add(server);
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, Servers.ToList(), JsonOptions, cancellationToken);
    }

    public async Task AddOrUpdateAsync(WinIrcServer server, CancellationToken cancellationToken = default)
    {
        var existing = Servers.FirstOrDefault(item =>
            string.Equals(item.Name, server.Name, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            Servers.Remove(existing);
        }

        Servers.Add(server);
        SortInPlace();
        await SaveAsync(cancellationToken);
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        var existing = Servers.FirstOrDefault(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            Servers.Remove(existing);
            await SaveAsync(cancellationToken);
        }
    }

    public WinIrcServer? Get(string name)
    {
        return Servers.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> ImportLegacyXmlAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        var imported = document.Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "IrcServer", StringComparison.OrdinalIgnoreCase))
            .Select(ParseLegacyServer)
            .Where(server => !string.IsNullOrWhiteSpace(server.Hostname))
            .ToList();

        foreach (var server in imported)
        {
            var existing = Servers.FirstOrDefault(item =>
                string.Equals(item.Name, server.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                Servers.Remove(existing);
            }

            Servers.Add(server);
        }

        SortInPlace();
        await SaveAsync(cancellationToken);
        return imported.Count;
    }

    private static WinIrcServer ParseLegacyServer(XElement element)
    {
        string Value(params string[] names)
        {
            return names
                .Select(name => element.Elements().FirstOrDefault(child =>
                    string.Equals(child.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value)
                .FirstOrDefault(value => value != null) ?? "";
        }

        _ = int.TryParse(Value("port", "Port"), out var port);
        _ = bool.TryParse(Value("ssl", "Ssl"), out var ssl);
        _ = bool.TryParse(Value("webSocket", "WebSocket"), out var webSocket);

        var hostname = Value("hostname", "Hostname");
        var name = Value("name", "Name");

        return new WinIrcServer
        {
            Name = string.IsNullOrWhiteSpace(name) ? hostname : name,
            Hostname = hostname,
            Port = port == 0 ? 6667 : port,
            Ssl = ssl,
            WebSocket = webSocket,
            Username = Value("username", "Username"),
            Password = Value("password", "Password"),
            NickservPassword = Value("nickservPassword", "NickservPassword"),
            Channels = Value("channels", "Channels")
        };
    }

    private void SortInPlace()
    {
        var sorted = Servers.OrderBy(server => server.Name, StringComparer.OrdinalIgnoreCase).ToList();
        Servers.Clear();

        foreach (var server in sorted)
        {
            Servers.Add(server);
        }
    }
}
