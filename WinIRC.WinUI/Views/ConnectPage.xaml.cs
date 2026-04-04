using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinIRC.Core;
using WinIRC.Core.Models;

namespace WinIRC.WinUI.Views;

public sealed partial class ConnectPage : UserControl, INotifyPropertyChanged
{
    private readonly AppBootstrapContext _context;
    private readonly Func<string?, SavedServer, Task> _saveServerAsync;
    private readonly Func<string, Task> _deleteServerAsync;
    private readonly Action<SavedServer> _openSession;
    private readonly Action? _closeDialog;
    private readonly string? _initialServerName;
    private string? _selectedServerName;
    private string _validationMessage = string.Empty;
    private bool _isLoadingSelection;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SavedServer> SavedServers { get; }

    public string ValidationMessage
    {
        get => _validationMessage;
        set
        {
            if (_validationMessage == value)
            {
                return;
            }

            _validationMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ValidationVisibility));
        }
    }

    public Visibility ValidationVisibility => ValidationMessage.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

    public ConnectPage(
        AppBootstrapContext context,
        ObservableCollection<SavedServer> savedServers,
        Func<string?, SavedServer, Task> saveServerAsync,
        Func<string, Task> deleteServerAsync,
        Action<SavedServer> openSession,
        Action? closeDialog = null,
        string? initialServerName = null)
    {
        _context = context;
        SavedServers = savedServers;
        _saveServerAsync = saveServerAsync;
        _deleteServerAsync = deleteServerAsync;
        _openSession = openSession;
        _closeDialog = closeDialog;
        _initialServerName = initialServerName;

        InitializeComponent();
        Loaded += ConnectPage_Loaded;
        ResetForm();
        UpdateDeleteState();
    }

    private void ConnectPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialServerName != null)
        {
            var selectedServer = SavedServers.FirstOrDefault(server => NamesMatch(server.Name, _initialServerName));
            if (selectedServer != null)
            {
                SavedServersCombo.SelectedItem = selectedServer;
                return;
            }
        }

        if (SavedServersCombo.SelectedItem == null && SavedServers.Count > 0)
        {
            SavedServersCombo.SelectedItem = SavedServers[0];
        }
    }

    private void SavedServersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SavedServersCombo.SelectedItem is not SavedServer server)
        {
            _selectedServerName = null;
            ResetForm();
            UpdateDeleteState();
            return;
        }

        LoadServer(server);
        UpdateDeleteState();
    }

    private void NewServer_Click(object sender, RoutedEventArgs e)
    {
        SavedServersCombo.SelectedItem = null;
        _selectedServerName = null;
        ResetForm();
        UpdateDeleteState();
    }

    private async void SaveServer_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildServerFromForm(out var server))
        {
            return;
        }

        if (!SavedServerValidation.TryValidateAndNormalize(server, out var errorMessage))
        {
            ValidationMessage = errorMessage;
            return;
        }

        ValidationMessage = string.Empty;
        await _saveServerAsync(_selectedServerName, server);
        _selectedServerName = server.Name;
        SavedServersCombo.SelectedItem = SavedServers.FirstOrDefault(item => NamesMatch(item.Name, server.Name));
        await _context.NotificationService.ShowInfoAsync("Saved network", server.Name);
    }

    private async void DeleteServer_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedServerName == null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Delete saved network?",
            Content = "This removes the saved network from the new WinUI local store.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        await _deleteServerAsync(_selectedServerName);
        ValidationMessage = string.Empty;
        _selectedServerName = null;
        SavedServersCombo.SelectedItem = null;
        ResetForm();
        UpdateDeleteState();
        await _context.NotificationService.ShowInfoAsync("Saved network removed", "The network was deleted from local state.");
    }

    private void OpenSession_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildServerFromForm(out var server))
        {
            return;
        }

        if (!SavedServerValidation.TryValidateAndNormalize(server, out var errorMessage))
        {
            ValidationMessage = errorMessage;
            return;
        }

        ValidationMessage = string.Empty;
        _openSession(server);
        _closeDialog?.Invoke();
    }

    private void WebSocketToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSelection)
        {
            return;
        }

        WebSocketInfoBar.IsOpen = WebSocketToggle.IsOn;
    }

    private void LoadServer(SavedServer server)
    {
        _isLoadingSelection = true;
        _selectedServerName = server.Name;
        ValidationMessage = string.Empty;
        ServerNameTextBox.Text = server.Name;
        HostnameTextBox.Text = server.Hostname;
        PortTextBox.Text = server.Port.ToString();
        ServerPasswordBox.Password = server.Password;
        SslToggle.IsOn = server.Ssl;
        WebSocketToggle.IsOn = server.UseWebSocket;
        AutoConnectToggle.IsOn = server.AutoConnect;
        UsernameTextBox.Text = server.Username;
        NickservPasswordBox.Password = server.NickservPassword;
        ChannelsTextBox.Text = server.Channels;
        WebSocketInfoBar.IsOpen = server.UseWebSocket;
        _isLoadingSelection = false;
    }

    private void ResetForm()
    {
        _isLoadingSelection = true;
        ValidationMessage = string.Empty;
        ServerNameTextBox.Text = string.Empty;
        HostnameTextBox.Text = string.Empty;
        PortTextBox.Text = "6667";
        ServerPasswordBox.Password = string.Empty;
        SslToggle.IsOn = false;
        WebSocketToggle.IsOn = false;
        AutoConnectToggle.IsOn = false;
        UsernameTextBox.Text = _context.SettingsStore.GetString(ConfigKeys.DefaultUsername, string.Empty);
        NickservPasswordBox.Password = string.Empty;
        ChannelsTextBox.Text = string.Empty;
        WebSocketInfoBar.IsOpen = false;
        _isLoadingSelection = false;
    }

    private bool TryBuildServerFromForm(out SavedServer server)
    {
        if (!int.TryParse(PortTextBox.Text.Trim(), out var port))
        {
            ValidationMessage = "Port is not a number.";
            server = new SavedServer();
            return false;
        }

        server = new SavedServer
        {
            Name = ServerNameTextBox.Text,
            Hostname = HostnameTextBox.Text,
            Port = port,
            Password = ServerPasswordBox.Password,
            Ssl = SslToggle.IsOn,
            UseWebSocket = WebSocketToggle.IsOn,
            AutoConnect = AutoConnectToggle.IsOn,
            Username = UsernameTextBox.Text,
            NickservPassword = NickservPasswordBox.Password,
            Channels = ChannelsTextBox.Text
        };

        return true;
    }

    private void UpdateDeleteState()
    {
        DeleteServerButton.IsEnabled = _selectedServerName != null;
    }

    private static bool NamesMatch(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
