using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinIRC.Core.Models;

namespace WinIRC.Core.Interfaces;

public interface ISettingsStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    bool Contains(string key);

    bool GetBoolean(string key, bool defaultValue = false);

    int GetInt(string key, int defaultValue = 0);

    string GetString(string key, string defaultValue = "");

    IReadOnlyDictionary<string, SettingValue> GetAll();

    Task ImportAsync(IReadOnlyDictionary<string, SettingValue> values, CancellationToken cancellationToken = default);

    Task SetBooleanAsync(string key, bool value, CancellationToken cancellationToken = default);

    Task SetIntAsync(string key, int value, CancellationToken cancellationToken = default);

    Task SetStringAsync(string key, string value, CancellationToken cancellationToken = default);
}
