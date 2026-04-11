namespace WinIRC.Core.Configuration;

public interface ISettingsStore
{
    bool Contains(string key);
    void SetString(string key, string value);
    void SetInt(string key, int value);
    void SetBoolean(string key, bool value);
    bool GetBoolean(string key, bool defaultValue = false);
    string GetString(string key, string defaultValue = "");
    int GetInt(string key, int defaultValue = 0);
    void RemoveKey(string key);
}
