using System;
using System.IO;

namespace WinIRC.WinUI.Services;

public static class StatePathResolver
{
    public static string GetStateDirectoryPath()
    {
        try
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "State");
        }
        catch
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "WinIRC", "State");
        }
    }
}
