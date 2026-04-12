namespace WinIRC.Core.Persistence;

public static class AppDataPaths
{
    public static string GetAppDataDirectory(string appName = "WinIRC")
    {
        string baseDirectory;

        if (OperatingSystem.IsLinux())
        {
            var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            baseDirectory = string.IsNullOrWhiteSpace(xdgConfigHome)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
                : xdgConfigHome;
        }
        else
        {
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        var appDirectory = Path.Combine(baseDirectory, appName);
        Directory.CreateDirectory(appDirectory);
        return appDirectory;
    }
}
