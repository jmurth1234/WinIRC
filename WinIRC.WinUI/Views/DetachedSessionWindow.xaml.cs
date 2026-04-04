using System.Linq;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinIRC.Core.Models;
using WinIRC.WinUI.Services;

namespace WinIRC.WinUI.Views;

public sealed partial class DetachedSessionWindow : Window
{
    private readonly ChatSessionDescriptor _descriptor;
    private bool _compactOverlay;

    public DetachedSessionWindow(ChatSessionDescriptor descriptor)
    {
        _descriptor = descriptor;
        InitializeComponent();
        ApplyThemeFromSettings();

        Title = descriptor.Header;
        HeaderText.Text = descriptor.Header;

        if (descriptor.Kind == ChatSessionKind.Settings)
        {
            ContentHost.Children.Add(new SettingsPage(App.Context, ApplyThemeFromSettingsAsync));
        }
        else
        {
            ContentHost.Children.Add(new ChannelPage(descriptor, null, App.Context));
        }
    }

    private System.Threading.Tasks.Task ApplyThemeFromSettingsAsync()
    {
        ApplyThemeFromSettings();
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private void ApplyThemeFromSettings()
    {
        var theme = App.Context.SettingsStore.GetBoolean(Core.ConfigKeys.DarkTheme, true)
            ? ElementTheme.Dark
            : ElementTheme.Light;

        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }

        if (ContentHost.Children.OfType<SettingsPage>().FirstOrDefault() is SettingsPage settingsPage)
        {
            settingsPage.RefreshFromSettings();
        }
    }

    private void CompactOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        var appWindow = WindowingHelpers.GetAppWindow(this);
        if (appWindow == null)
        {
            return;
        }

        _compactOverlay = !_compactOverlay;
        appWindow.SetPresenter(_compactOverlay ? AppWindowPresenterKind.CompactOverlay : AppWindowPresenterKind.Default);
        CompactOverlayButton.Content = _compactOverlay ? "Standard Window" : "Compact Overlay";
    }
}
