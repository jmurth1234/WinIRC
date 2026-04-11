using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using WinIRC.Avalonia.ViewModels;
using WinIRC.Core.Models;
using WinIRC.Core.Persistence;

namespace WinIRC.Avalonia;

public partial class ServerDialog : Window
{
    private readonly ServerDialogViewModel viewModel;

    public ServerDialog() : this(
        new ServerStore(AppDataPaths.GetAppDataDirectory()),
        new JsonSettingsStore(AppDataPaths.GetAppDataDirectory()))
    {
    }

    public ServerDialog(ServerStore serverStore, JsonSettingsStore settings)
    {
        InitializeComponent();
        viewModel = new ServerDialogViewModel(serverStore, settings);
        DataContext = viewModel;
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        await viewModel.SaveAsync();
    }

    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        await viewModel.DeleteSelectedAsync();
    }

    private void New_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.NewServer();
    }

    private async void Import_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import WinIRC servers",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("WinIRC XML") { Patterns = ["*.xml"] },
                FilePickerFileTypes.All
            ]
        });

        var file = files.FirstOrDefault();
        if (file == null)
        {
            return;
        }

        await using var stream = await file.OpenReadAsync();
        await viewModel.ImportLegacyAsync(stream);
    }

    private void Connect_Click(object? sender, RoutedEventArgs e)
    {
        var server = viewModel.BuildServer();
        if (server != null)
        {
            Close(server);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close((WinIrcServer?)null);
    }
}
