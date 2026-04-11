using WinIRC.Core.Configuration;
using WinIRC.Core.Persistence;

namespace WinIRC.Tests.Core;

public sealed class JsonSettingsStoreTests
{
    [Fact]
    public void SettingsPersistAcrossStoreInstances()
    {
        var directory = CreateTempDirectory();
        var store = new JsonSettingsStore(directory);

        store.SetBoolean(ConfigKeys.SwitchOnJoin, false);
        store.SetInt(ConfigKeys.UserListClick, 1);
        store.SetString(ConfigKeys.UploadFile, "https://uploads.example");
        store.SetString(ConfigKeys.LogsFolder, Path.Combine(directory, "logs"));

        var reloaded = new JsonSettingsStore(directory);

        Assert.False(reloaded.GetBoolean(ConfigKeys.SwitchOnJoin, true));
        Assert.Equal(1, reloaded.GetInt(ConfigKeys.UserListClick));
        Assert.Equal("https://uploads.example", reloaded.GetString(ConfigKeys.UploadFile));
        Assert.Equal(Path.Combine(directory, "logs"), reloaded.GetString(ConfigKeys.LogsFolder));
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winirc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
