using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WinIRC.Avalonia;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        VersionText.Text = "WinIRC v" + GetVersion();
    }

    private static string GetVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version == null
            ? "dev"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private void Website_Click(object? sender, RoutedEventArgs e)
    {
        OpenUri("https://rymate.co.uk");
    }

    private void GitHub_Click(object? sender, RoutedEventArgs e)
    {
        OpenUri("https://github.com/rymate1234/WinIRC/issues/");
    }

    private void JoinSupport_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private static void OpenUri(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
        }
    }
}
