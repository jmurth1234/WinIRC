using Avalonia.Controls;
using Avalonia.Interactivity;
using WinIRC.Avalonia.ViewModels;
using WinIRC.Core.Persistence;

namespace WinIRC.Avalonia;

public partial class ChannelListDialog : Window
{
    private readonly MainWindowViewModel viewModel;

    public ChannelListDialog() : this(
        new MainWindowViewModel(
            new JsonSettingsStore(AppDataPaths.GetAppDataDirectory()),
            new ServerStore(AppDataPaths.GetAppDataDirectory())))
    {
    }

    public ChannelListDialog(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        await viewModel.RequestChannelListAsync();
    }

    private async void Join_Click(object? sender, RoutedEventArgs e)
    {
        if (ChannelsList.SelectedItem is ChannelListEntryViewModel channel)
        {
            await viewModel.JoinListedChannelAsync(channel);
            Close();
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
