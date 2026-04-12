using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using WinIRC.Avalonia.ViewModels;
using WinIRC.Core.Persistence;

namespace WinIRC.Avalonia;

public partial class SettingsDialog : Window
{
    private readonly MainWindowViewModel viewModel;

    public SettingsDialog() : this(
        new MainWindowViewModel(
            new JsonSettingsStore(AppDataPaths.GetAppDataDirectory()),
            new ServerStore(AppDataPaths.GetAppDataDirectory())))
    {
    }

    public SettingsDialog(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
    }

    private void SmallerText_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.DecreaseFontSize();
    }

    private void LargerText_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.IncreaseFontSize();
    }

    private async void ChooseLogsFolder_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose Logs Folder",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder?.Path.LocalPath is { Length: > 0 } path)
        {
            viewModel.SetLogsFolderPath(path);
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
