using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WinIRC.Core.Interfaces;
using WinIRC.Core.Models;

namespace WinIRC.WinUI.Services;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    private SettingsSnapshot _snapshot = new SettingsSnapshot();
    private string _filePath = string.Empty;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _filePath = Path.Combine(StatePathResolver.GetStateDirectoryPath(), "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        if (!File.Exists(_filePath))
        {
            return;
        }

        await using var stream = File.OpenRead(_filePath);
        _snapshot = await JsonSerializer.DeserializeAsync<SettingsSnapshot>(stream, _serializerOptions, cancellationToken).ConfigureAwait(false) ?? new SettingsSnapshot();
    }

    public bool Contains(string key)
    {
        return _snapshot.Values.ContainsKey(key);
    }

    public bool GetBoolean(string key, bool defaultValue = false)
    {
        return _snapshot.Values.TryGetValue(key, out var value) && value.BoolValue.HasValue
            ? value.BoolValue.Value
            : defaultValue;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return _snapshot.Values.TryGetValue(key, out var value) && value.IntValue.HasValue
            ? value.IntValue.Value
            : defaultValue;
    }

    public string GetString(string key, string defaultValue = "")
    {
        return _snapshot.Values.TryGetValue(key, out var value) && value.StringValue != null
            ? value.StringValue
            : defaultValue;
    }

    public IReadOnlyDictionary<string, SettingValue> GetAll()
    {
        return _snapshot.Values;
    }

    public async Task ImportAsync(IReadOnlyDictionary<string, SettingValue> values, CancellationToken cancellationToken = default)
    {
        foreach (var pair in values)
        {
            _snapshot.Values[pair.Key] = pair.Value;
        }

        await SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetBooleanAsync(string key, bool value, CancellationToken cancellationToken = default)
    {
        _snapshot.Values[key] = SettingValue.FromBoolean(value);
        await SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetIntAsync(string key, int value, CancellationToken cancellationToken = default)
    {
        _snapshot.Values[key] = SettingValue.FromInt(value);
        await SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetStringAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _snapshot.Values[key] = SettingValue.FromString(value);
        await SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, _snapshot, _serializerOptions, cancellationToken).ConfigureAwait(false);
    }
}
