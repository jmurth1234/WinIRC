using System.Text.Json;
using WinIRC.Core.Configuration;

namespace WinIRC.Core.Persistence;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string filePath;
    private readonly object gate = new();
    private Dictionary<string, JsonElement> values = new(StringComparer.OrdinalIgnoreCase);

    public JsonSettingsStore(string appDataDirectory)
    {
        Directory.CreateDirectory(appDataDirectory);
        filePath = Path.Combine(appDataDirectory, "settings.json");
        Load();
    }

    public bool Contains(string key)
    {
        lock (gate)
        {
            return values.ContainsKey(key);
        }
    }

    public void SetString(string key, string value)
    {
        Set(key, value);
    }

    public void SetInt(string key, int value)
    {
        Set(key, value);
    }

    public void SetBoolean(string key, bool value)
    {
        Set(key, value);
    }

    public bool GetBoolean(string key, bool defaultValue = false)
    {
        lock (gate)
        {
            return values.TryGetValue(key, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? value.GetBoolean()
                : defaultValue;
        }
    }

    public string GetString(string key, string defaultValue = "")
    {
        lock (gate)
        {
            return values.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? defaultValue
                : defaultValue;
        }
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        lock (gate)
        {
            return values.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue)
                ? intValue
                : defaultValue;
        }
    }

    public void RemoveKey(string key)
    {
        lock (gate)
        {
            if (values.Remove(key))
            {
                Save();
            }
        }
    }

    private void Set<T>(string key, T value)
    {
        lock (gate)
        {
            values[key] = JsonSerializer.SerializeToElement(value);
            Save();
        }
    }

    private void Load()
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var json = File.ReadAllText(filePath);
        values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new Dictionary<string, JsonElement>();
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }
}
