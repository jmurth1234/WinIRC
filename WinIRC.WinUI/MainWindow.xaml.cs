using System;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinIRC.WinUI.Services;
using Windows.UI;

namespace WinIRC.WinUI;

public sealed partial class MainWindow : Window
{
    private static readonly string StartupLogPath = Path.Combine(Path.GetTempPath(), "WinIRC.WinUI.startup.log");
    private ShellPage? _shellPage;

    public MainWindow()
    {
        InitializeComponent();
        LoadShell();
    }

    private void LoadShell()
    {
#if DEBUG
        try
        {
            if (File.Exists(StartupLogPath))
            {
                File.Delete(StartupLogPath);
            }

            _shellPage = new ShellPage(App.Context);
            RootLayout.Children.Add(_shellPage);
            ConfigureTitleBar();
            ApplyTheme();
        }
        catch (Exception exception)
        {
            var details = $"{exception}{Environment.NewLine}HResult=0x{exception.HResult:X8}";
            File.WriteAllText(StartupLogPath, details);
            RootLayout.Children.Add(new ScrollViewer
            {
                Content = new TextBlock
                {
                    Margin = new Thickness(24),
                    Text = details,
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }
#else
        _shellPage = new ShellPage(App.Context);
        RootLayout.Children.Add(_shellPage);
        ConfigureTitleBar();
        ApplyTheme();
#endif
    }

    public void ApplyTheme()
    {
        var theme = App.Context.SettingsStore.GetBoolean(Core.ConfigKeys.DarkTheme, true)
            ? ElementTheme.Dark
            : ElementTheme.Light;

        RootLayout.RequestedTheme = theme;

        foreach (var child in RootLayout.Children.OfType<FrameworkElement>())
        {
            child.RequestedTheme = theme;
        }

        ApplyTitleBarTheme(theme);
        _shellPage?.ApplyTheme(theme);
    }

    private void ConfigureTitleBar()
    {
        if (_shellPage?.TitleBarElement == null)
        {
            return;
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(_shellPage.TitleBarElement);
        ApplyTitleBarTheme(RootLayout.RequestedTheme);
    }

    private void ApplyTitleBarTheme(ElementTheme theme)
    {
        var titleBar = WindowingHelpers.GetAppWindow(this)?.TitleBar;
        if (titleBar == null)
        {
            return;
        }

        var foreground = theme == ElementTheme.Dark
            ? Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0xFF, 0x18, 0x18, 0x18);
        var inactiveForeground = theme == ElementTheme.Dark
            ? Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x99, 0x18, 0x18, 0x18);
        var hoverBackground = theme == ElementTheme.Dark
            ? Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x14, 0x00, 0x00, 0x00);
        var pressedBackground = theme == ElementTheme.Dark
            ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x1F, 0x00, 0x00, 0x00);

        titleBar.ButtonBackgroundColor = Color.FromArgb(0x00, 0x00, 0x00, 0x00);
        titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0x00, 0x00, 0x00, 0x00);
        titleBar.ButtonForegroundColor = foreground;
        titleBar.ButtonInactiveForegroundColor = inactiveForeground;
        titleBar.ButtonHoverForegroundColor = foreground;
        titleBar.ButtonHoverBackgroundColor = hoverBackground;
        titleBar.ButtonPressedForegroundColor = foreground;
        titleBar.ButtonPressedBackgroundColor = pressedBackground;
    }
}
