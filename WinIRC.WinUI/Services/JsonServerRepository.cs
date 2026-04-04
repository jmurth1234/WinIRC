using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WinIRC.Core.Interfaces;
using WinIRC.Core.Models;

namespace WinIRC.WinUI.Services;

public sealed class JsonServerRepository : IServerRepository
{
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    private string _filePath = string.Empty;
    private List<SavedServer> _servers = new List<SavedServer>();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _filePath = Path.Combine(StatePathResolver.GetStateDirectoryPath(), "servers.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        if (!File.Exists(_filePath))
        {
            return;
        }

        await using var stream = File.OpenRead(_filePath);
        var servers = await JsonSerializer.DeserializeAsync<List<SavedServer>>(stream, _serializerOptions, cancellationToken).ConfigureAwait(false);
        _servers = servers ?? new List<SavedServer>();
    }

    public Task<IReadOnlyList<SavedServer>> LoadAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SavedServer> snapshot = _servers.ToList();
        return Task.FromResult(snapshot);
    }

    public async Task SaveAsync(IEnumerable<SavedServer> servers, CancellationToken cancellationToken = default)
    {
        _servers = servers.ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, _servers, _serializerOptions, cancellationToken).ConfigureAwait(false);
    }
}
