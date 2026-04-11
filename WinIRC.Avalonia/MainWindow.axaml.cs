using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using WinIRC.Avalonia.ViewModels;
using WinIRC.Core.Persistence;

namespace WinIRC.Avalonia;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var appDataDirectory = AppDataPaths.GetAppDataDirectory();
        var settings = new JsonSettingsStore(appDataDirectory);
        var serverStore = new ServerStore(appDataDirectory);

        viewModel = new MainWindowViewModel(settings, serverStore);
        DataContext = viewModel;
        viewModel.CurrentMessages.CollectionChanged += (_, _) =>
        {
            if (viewModel.CurrentMessages.Count == 0)
            {
                return;
            }

            var last = viewModel.CurrentMessages[^1];
            Dispatcher.UIThread.Post(() => MessagesList.ScrollIntoView(last));
        };

        Opened += async (_, _) => await viewModel.LoadAsync();
    }

    private async void ManageServers_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new ServerDialog(viewModel.ServerStore, viewModel.Settings);
        var server = await dialog.ShowDialog<WinIRC.Core.Models.WinIrcServer?>(this);

        if (server != null)
        {
            await viewModel.ConnectAsync(server);
        }
    }

    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new SettingsDialog(viewModel);
        await dialog.ShowDialog(this);
    }

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialog();
        if (await dialog.ShowDialog<bool>(this))
        {
            await viewModel.ConnectSupportAsync();
        }
    }

    private async void ConnectAll_Click(object? sender, RoutedEventArgs e)
    {
        await viewModel.ConnectAllSavedAsync();
    }

    private async void Disconnect_Click(object? sender, RoutedEventArgs e)
    {
        await viewModel.DisconnectSelectedAsync();
    }

    private async void Reconnect_Click(object? sender, RoutedEventArgs e)
    {
        await viewModel.ReconnectSelectedAsync();
    }

    private async void JoinChannel_Click(object? sender, RoutedEventArgs e)
    {
        await PromptJoinChannelAsync();
    }

    private async void ChannelList_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new ChannelListDialog(viewModel);
        await viewModel.RequestChannelListAsync();
        await dialog.ShowDialog(this);
    }

    private async Task PromptJoinChannelAsync()
    {
        var dialog = new TextPromptDialog("Join Channel", "Channel", "#channel");
        var channel = await dialog.ShowDialog<string>(this);
        await viewModel.JoinChannelAsync(channel);
    }

    private async void PartChannel_Click(object? sender, RoutedEventArgs e)
    {
        await viewModel.PartSelectedChannelAsync();
    }

    private void LargerText_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.IncreaseFontSize();
    }

    private void SmallerText_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.DecreaseFontSize();
    }

    private void OpenLogsFolder_Click(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(viewModel.LogsFolderPath);
        Process.Start(new ProcessStartInfo(viewModel.LogsFolderPath) { UseShellExecute = true });
    }

    private void Quit_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SendButton_Click(object? sender, RoutedEventArgs e)
    {
        await viewModel.SendCurrentAsync();
    }

    private async void UploadFile_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Upload File",
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.All]
        });

        var file = files.FirstOrDefault();
        if (file == null)
        {
            return;
        }

        await using var stream = await file.OpenReadAsync();
        await viewModel.UploadFileToInputAsync(stream, file.Name);
        InputBox.Focus();
        InputBox.CaretIndex = viewModel.InputText.Length;
    }

    private async void InputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            viewModel.ResetTabCompletion();
            await viewModel.SendCurrentAsync();
            return;
        }

        if (e.Key == Key.Up || e.Key == Key.Down)
        {
            if (viewModel.TryNavigateInputHistory(e.Key == Key.Up) && sender is TextBox historyTextBox)
            {
                e.Handled = true;
                historyTextBox.Text = viewModel.InputText;
                historyTextBox.CaretIndex = viewModel.InputText.Length;
            }

            return;
        }

        if (e.Key == Key.Tab)
        {
            if (sender is TextBox textBox)
            {
                var caret = viewModel.TryApplyTabCompletion(textBox.CaretIndex);
                if (caret.HasValue)
                {
                    e.Handled = true;
                    textBox.Text = viewModel.InputText;
                    textBox.CaretIndex = caret.Value;
                }
            }

            return;
        }

        viewModel.ResetTabCompletion();
    }

    private async void InputBox_PastingFromClipboard(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        var text = clipboard == null ? null : await clipboard.TryGetTextAsync();
        if (string.IsNullOrEmpty(text) || !HasMultipleLines(text))
        {
            return;
        }

        e.Handled = true;
        var dialog = new PasteTextDialog(text);
        var result = await dialog.ShowDialog<PasteTextResult?>(this);
        if (result == null || result.Action == PasteTextAction.Cancel)
        {
            return;
        }

        if (result.Action == PasteTextAction.Upload)
        {
            await viewModel.UploadTextToInputAsync(result.Text);
        }
        else
        {
            viewModel.AppendPastedTextToInput(result.Text);
        }

        InputBox.Focus();
        InputBox.CaretIndex = viewModel.InputText.Length;
    }

    private async void CopyMessage_Click(object? sender, RoutedEventArgs e)
    {
        if (GetMessage(sender) is { } message)
        {
            await CopyTextAsync(FormatMessage(message));
        }
    }

    private async void CopyLink_Click(object? sender, RoutedEventArgs e)
    {
        if (GetMessage(sender) is { HasLink: true } message)
        {
            await CopyTextAsync(message.FirstLink);
        }
    }

    private void OpenLink_Click(object? sender, RoutedEventArgs e)
    {
        if (GetMessage(sender) is not { HasLink: true } message)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(message.FirstLink) { UseShellExecute = true });
        }
        catch
        {
            _ = CopyTextAsync(message.FirstLink);
        }
    }

    private static MessageViewModel? GetMessage(object? sender)
    {
        return sender is MenuItem { CommandParameter: MessageViewModel message } ? message : null;
    }

    private static string FormatMessage(MessageViewModel message)
    {
        return string.IsNullOrWhiteSpace(message.User)
            ? $"{message.Time} {message.Text}"
            : $"{message.Time} <{message.User}> {message.Text}";
    }

    private async Task CopyTextAsync(string text)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    private async void SidebarJoin_Click(object? sender, RoutedEventArgs e)
    {
        if (GetSidebarItem(sender) is { } item)
        {
            viewModel.SelectedItem = item;
        }

        await PromptJoinChannelAsync();
    }

    private async void SidebarPart_Click(object? sender, RoutedEventArgs e)
    {
        if (GetSidebarItem(sender) is { } item)
        {
            viewModel.SelectedItem = item;
        }

        await viewModel.PartSelectedChannelAsync();
    }

    private async void SidebarReconnect_Click(object? sender, RoutedEventArgs e)
    {
        if (GetSidebarItem(sender) is { } item)
        {
            viewModel.SelectedItem = item;
        }

        await viewModel.ReconnectSelectedAsync();
    }

    private async void SidebarDisconnect_Click(object? sender, RoutedEventArgs e)
    {
        if (GetSidebarItem(sender) is { } item)
        {
            viewModel.SelectedItem = item;
        }

        await viewModel.DisconnectSelectedAsync();
    }

    private async void CopySidebarName_Click(object? sender, RoutedEventArgs e)
    {
        if (GetSidebarItem(sender) is { } item)
        {
            await CopyTextAsync(item.IsServer ? item.Session.Server.Name : item.Channel);
        }
    }

    private void MentionUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            viewModel.MentionUser(user);
            InputBox.Focus();
            InputBox.CaretIndex = viewModel.InputText.Length;
        }
    }

    private async void CopyUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await CopyTextAsync(user);
        }
    }

    private async void UsersList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (UsersList.SelectedItem is string user)
        {
            await viewModel.ActivateUserAsync(user);
            InputBox.Focus();
            InputBox.CaretIndex = viewModel.InputText.Length;
        }
    }

    private async void QueryUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await viewModel.QueryUserAsync(user);
        }
    }

    private async void WhoisUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await viewModel.WhoisUserAsync(user);
        }
    }

    private async void OpUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await viewModel.OpUserAsync(user);
        }
    }

    private async void DeopUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await viewModel.DeopUserAsync(user);
        }
    }

    private async void VoiceUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await viewModel.VoiceUserAsync(user);
        }
    }

    private async void DevoiceUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await viewModel.DevoiceUserAsync(user);
        }
    }

    private async void MuteUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await viewModel.MuteUserAsync(user);
        }
    }

    private async void UnmuteUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await viewModel.UnmuteUserAsync(user);
        }
    }

    private async void KickUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await viewModel.KickUserAsync(user);
        }
    }

    private async void BanUser_Click(object? sender, RoutedEventArgs e)
    {
        if (GetUser(sender) is { } user)
        {
            await viewModel.BanUserAsync(user);
        }
    }

    private static SidebarItemViewModel? GetSidebarItem(object? sender)
    {
        return sender is MenuItem { CommandParameter: SidebarItemViewModel item } ? item : null;
    }

    private static string? GetUser(object? sender)
    {
        return sender is MenuItem { CommandParameter: string user } ? user : null;
    }

    private static bool HasMultipleLines(string text)
    {
        return text.Contains('\n') || text.Contains('\r');
    }
}
