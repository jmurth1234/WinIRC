using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace WinIRC.WinUI;

public partial class App : Application
{
    public static Window MainWindow { get; private set; } = null!;

    public static AppBootstrapContext Context { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        AppInstance.GetCurrent().Activated += OnAppInstanceActivated;
        UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        await EnsureInitializedAsync(GetPendingProtocolUri());
    }

    private static async Task EnsureInitializedAsync(Uri? pendingProtocolUri = null)
    {
        if (Context is null)
        {
            Context = await AppBootstrapContext.CreateAsync().ConfigureAwait(true);
        }

        if (pendingProtocolUri != null)
        {
            Context.ProtocolActivationService.SetPendingProtocolUri(pendingProtocolUri);
        }

        if (MainWindow is null)
        {
            MainWindow = new MainWindow();
        }

        MainWindow.Activate();
    }

    private async void OnAppInstanceActivated(object? sender, AppActivationArguments args)
    {
        await EnsureInitializedAsync(GetPendingProtocolUri(args));
    }

    private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        await WriteCrashLogAsync("Application.UnhandledException", e.Exception);
    }

    private async void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        await WriteCrashLogAsync("TaskScheduler.UnobservedTaskException", e.Exception);
    }

    private async void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        await WriteCrashLogAsync("AppDomain.UnhandledException", e.ExceptionObject as Exception);
    }

    private static Uri? GetPendingProtocolUri()
    {
        return GetPendingProtocolUri(AppInstance.GetCurrent().GetActivatedEventArgs());
    }

    private static Uri? GetPendingProtocolUri(AppActivationArguments? activationArgs)
    {
        if (activationArgs?.Kind == ExtendedActivationKind.Protocol && activationArgs.Data is ProtocolActivatedEventArgs protocolArgs)
        {
            return protocolArgs.Uri;
        }

        return null;
    }

    private static async Task WriteCrashLogAsync(string source, Exception? exception)
    {
        try
        {
            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("State", CreationCollisionOption.OpenIfExists);
            var file = await folder.CreateFileAsync("crash.log", CreationCollisionOption.OpenIfExists);

            var builder = new StringBuilder();
            builder.AppendLine($"Timestamp: {DateTimeOffset.Now:O}");
            builder.AppendLine($"Source: {source}");
            if (exception != null)
            {
                builder.AppendLine($"Type: {exception.GetType().FullName}");
                builder.AppendLine($"Message: {exception.Message}");
                builder.AppendLine(exception.ToString());
            }
            else
            {
                builder.AppendLine("Exception: <null>");
            }

            builder.AppendLine(new string('-', 60));
            await FileIO.AppendTextAsync(file, builder.ToString());
        }
        catch
        {
            var fallbackPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinIRC.WinUI.crash.log");

            var builder = new StringBuilder();
            builder.AppendLine($"Timestamp: {DateTimeOffset.Now:O}");
            builder.AppendLine($"Source: {source}");
            builder.AppendLine(exception?.ToString() ?? "<null>");
            builder.AppendLine(new string('-', 60));
            File.AppendAllText(fallbackPath, builder.ToString());
        }
    }
}
