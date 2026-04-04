using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel;
using WinIRC.Core;

namespace WinIRC.WinUI.Views;

public sealed partial class SettingsPage : UserControl, INotifyPropertyChanged
{
    private readonly AppBootstrapContext _context;
    private readonly Func<Task> _applySettingsCallback;
    private readonly SettingsSection _initialSection;
    private bool _isDarkTheme;
    private bool _isModernChat;
    private bool _ignoreJoinLeave;
    private bool _reducedPadding;
    private bool _ignoreSsl;
    private bool _autoReconnect;
    private bool _enableLogs;
    private bool _isInitializing;
    private string _logsFolderText = "(not set)";
    private string _chatFontFamily = "Cascadia Mono";
    private double _chatFontSize = 13;
    private string _defaultUsername = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> ChatFonts { get; } = new()
    {
        "Cascadia Mono",
        "Consolas",
        "Courier New",
        "Lucida Console",
        "Segoe UI",
        "Segoe UI Variable Text"
    };

    public ObservableCollection<string> UserListClickModes { get; } = new()
    {
        "Mention user in channel",
        "PM the user",
        "Show the context menu"
    };

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme == value)
            {
                return;
            }

            _isDarkTheme = value;
            OnPropertyChanged();
        }
    }

    public string LogsFolderText
    {
        get => _logsFolderText;
        set
        {
            if (_logsFolderText == value)
            {
                return;
            }

            _logsFolderText = value;
            OnPropertyChanged();
        }
    }

    public bool IsModernChat
    {
        get => _isModernChat;
        set
        {
            if (_isModernChat == value)
            {
                return;
            }

            _isModernChat = value;
            OnPropertyChanged();
        }
    }

    public bool IgnoreJoinLeave
    {
        get => _ignoreJoinLeave;
        set
        {
            if (_ignoreJoinLeave == value)
            {
                return;
            }

            _ignoreJoinLeave = value;
            OnPropertyChanged();
        }
    }

    public bool ReducedPadding
    {
        get => _reducedPadding;
        set
        {
            if (_reducedPadding == value)
            {
                return;
            }

            _reducedPadding = value;
            OnPropertyChanged();
        }
    }

    public bool IgnoreSsl
    {
        get => _ignoreSsl;
        set
        {
            if (_ignoreSsl == value)
            {
                return;
            }

            _ignoreSsl = value;
            OnPropertyChanged();
        }
    }

    public bool AutoReconnect
    {
        get => _autoReconnect;
        set
        {
            if (_autoReconnect == value)
            {
                return;
            }

            _autoReconnect = value;
            OnPropertyChanged();
        }
    }

    public bool EnableLogs
    {
        get => _enableLogs;
        set
        {
            if (_enableLogs == value)
            {
                return;
            }

            _enableLogs = value;
            OnPropertyChanged();
        }
    }

    public string ChatFontFamily
    {
        get => _chatFontFamily;
        set
        {
            if (_chatFontFamily == value)
            {
                return;
            }

            _chatFontFamily = value;
            OnPropertyChanged();
        }
    }

    public double ChatFontSize
    {
        get => _chatFontSize;
        set
        {
            if (Math.Abs(_chatFontSize - value) < 0.1)
            {
                return;
            }

            _chatFontSize = value;
            OnPropertyChanged();
        }
    }

    public string DefaultUsername
    {
        get => _defaultUsername;
        set
        {
            if (_defaultUsername == value)
            {
                return;
            }

            _defaultUsername = value;
            OnPropertyChanged();
        }
    }

    public string AboutVersionText => "Version " + GetVersionText();

    public SettingsPage(AppBootstrapContext context, Func<Task> applySettingsCallback, SettingsSection initialSection = SettingsSection.General)
    {
        _context = context;
        _applySettingsCallback = applySettingsCallback;
        _initialSection = initialSection;
        _isInitializing = true;
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
        RefreshFromSettings();
        LogsFolderText = _context.SettingsStore.GetString(ConfigKeys.LogsFolder, "(not set)");
        _isInitializing = false;
    }

    public void RefreshFromSettings()
    {
        var isDarkTheme = _context.SettingsStore.GetBoolean(ConfigKeys.DarkTheme, true);
        IsDarkTheme = isDarkTheme;
        IsModernChat = _context.SettingsStore.GetBoolean(ConfigKeys.ModernChat, true);
        IgnoreJoinLeave = _context.SettingsStore.GetBoolean(ConfigKeys.IgnoreJoinLeave, false);
        ReducedPadding = _context.SettingsStore.GetBoolean(ConfigKeys.ReducedPadding, false);
        IgnoreSsl = _context.SettingsStore.GetBoolean(ConfigKeys.IgnoreSSL, false);
        AutoReconnect = _context.SettingsStore.GetBoolean(ConfigKeys.AutoReconnect, true);
        EnableLogs = _context.SettingsStore.GetBoolean(ConfigKeys.EnableLogs, false);
        ChatFontFamily = _context.SettingsStore.GetString(ConfigKeys.FontFamily, "Cascadia Mono");
        ChatFontSize = _context.SettingsStore.GetInt(ConfigKeys.FontSize, 13);
        DefaultUsername = _context.SettingsStore.GetString(ConfigKeys.DefaultUsername, string.Empty);
        LogsFolderText = _context.SettingsStore.GetString(ConfigKeys.LogsFolder, "(not set)");

        if (DarkThemeToggle != null)
        {
            DarkThemeToggle.IsOn = isDarkTheme;
        }

        if (ModernChatToggle != null)
        {
            ModernChatToggle.IsOn = IsModernChat;
        }

        if (IgnoreJoinLeaveToggle != null)
        {
            IgnoreJoinLeaveToggle.IsOn = IgnoreJoinLeave;
        }

        if (UserListClickComboBox != null)
        {
            var selectedIndex = _context.SettingsStore.GetInt(ConfigKeys.UserListClick, 0);
            UserListClickComboBox.SelectedIndex = Math.Clamp(selectedIndex, 0, UserListClickModes.Count - 1);
        }

        if (ReducedPaddingToggle != null)
        {
            ReducedPaddingToggle.IsOn = ReducedPadding;
        }

        if (IgnoreSslToggle != null)
        {
            IgnoreSslToggle.IsOn = IgnoreSsl;
        }

        if (AutoReconnectToggle != null)
        {
            AutoReconnectToggle.IsOn = AutoReconnect;
        }

        if (EnableLogsToggle != null)
        {
            EnableLogsToggle.IsOn = EnableLogs;
        }

        if (FontComboBox != null)
        {
            FontComboBox.SelectedItem = ChatFonts.Contains(ChatFontFamily) ? ChatFontFamily : "Cascadia Mono";
        }

        if (FontSizeNumberBox != null)
        {
            FontSizeNumberBox.Value = ChatFontSize;
        }

        if (DefaultUsernameTextBox != null)
        {
            DefaultUsernameTextBox.Text = DefaultUsername;
        }

        if (ChooseLogsFolderButton != null)
        {
            ChooseLogsFolderButton.IsEnabled = EnableLogs;
        }
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        ScrollToInitialSection();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void DarkThemeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        IsDarkTheme = DarkThemeToggle.IsOn;
        await _context.SettingsStore.SetBooleanAsync(ConfigKeys.DarkTheme, IsDarkTheme);
        await _applySettingsCallback();
    }

    private async void ModernChatToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        IsModernChat = ModernChatToggle.IsOn;
        await _context.SettingsStore.SetBooleanAsync(ConfigKeys.ModernChat, IsModernChat);
        await _applySettingsCallback();
    }

    private async void IgnoreJoinLeaveToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        IgnoreJoinLeave = IgnoreJoinLeaveToggle.IsOn;
        await _context.SettingsStore.SetBooleanAsync(ConfigKeys.IgnoreJoinLeave, IgnoreJoinLeave);
        await _applySettingsCallback();
    }

    private async void ReducedPaddingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        ReducedPadding = ReducedPaddingToggle.IsOn;
        await _context.SettingsStore.SetBooleanAsync(ConfigKeys.ReducedPadding, ReducedPadding);
        await _applySettingsCallback();
    }

    private async void UserListClickComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || UserListClickComboBox.SelectedIndex < 0)
        {
            return;
        }

        await _context.SettingsStore.SetIntAsync(ConfigKeys.UserListClick, UserListClickComboBox.SelectedIndex);
    }

    private async void IgnoreSslToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        IgnoreSsl = IgnoreSslToggle.IsOn;
        await _context.SettingsStore.SetBooleanAsync(ConfigKeys.IgnoreSSL, IgnoreSsl);
    }

    private async void AutoReconnectToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        AutoReconnect = AutoReconnectToggle.IsOn;
        await _context.SettingsStore.SetBooleanAsync(ConfigKeys.AutoReconnect, AutoReconnect);
    }

    private async void EnableLogsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        EnableLogs = EnableLogsToggle.IsOn;
        await _context.SettingsStore.SetBooleanAsync(ConfigKeys.EnableLogs, EnableLogs);

        if (ChooseLogsFolderButton != null)
        {
            ChooseLogsFolderButton.IsEnabled = EnableLogs;
        }

        if (EnableLogs && string.IsNullOrWhiteSpace(_context.SettingsStore.GetString(ConfigKeys.LogsFolder, string.Empty)))
        {
            await ChooseLogsFolderAsync();
        }
    }

    private async void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || FontComboBox.SelectedItem is not string selectedFont)
        {
            return;
        }

        ChatFontFamily = selectedFont;
        await _context.SettingsStore.SetStringAsync(ConfigKeys.FontFamily, selectedFont);
        await _applySettingsCallback();
    }

    private async void FontSizeNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isInitializing || double.IsNaN(sender.Value))
        {
            return;
        }

        var fontSize = Math.Clamp((int)Math.Round(sender.Value), 8, 30);
        ChatFontSize = fontSize;
        await _context.SettingsStore.SetIntAsync(ConfigKeys.FontSize, fontSize);
        await _applySettingsCallback();
    }

    private async void DefaultUsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        await SaveDefaultUsernameAsync();
    }

    private async void DefaultUsernameButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveDefaultUsernameAsync();
    }

    private async void ChooseLogsFolder_Click(object sender, RoutedEventArgs e)
    {
        await ChooseLogsFolderAsync();
    }

    private async void TestNotification_Click(object sender, RoutedEventArgs e)
    {
        await _context.NotificationService.ShowInfoAsync("WinIRC", "Desktop notifications are enabled.");
    }

    private void ScrollToInitialSection()
    {
        FrameworkElement? target = _initialSection switch
        {
            SettingsSection.Appearance => AppearanceSection,
            SettingsSection.Connection => ConnectionSection,
            SettingsSection.Behaviour => BehaviourSection,
            SettingsSection.About => AboutSection,
            _ => null
        };

        if (target == null)
        {
            return;
        }

        target.StartBringIntoView(new BringIntoViewOptions
        {
            AnimationDesired = false
        });
    }

    private async Task SaveDefaultUsernameAsync()
    {
        if (_isInitializing)
        {
            return;
        }

        DefaultUsername = DefaultUsernameTextBox.Text;
        await _context.SettingsStore.SetStringAsync(ConfigKeys.DefaultUsername, DefaultUsername);
    }

    private async Task ChooseLogsFolderAsync()
    {
        var folder = await _context.FilePickerService.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        LogsFolderText = folder;
        await _context.SettingsStore.SetStringAsync(ConfigKeys.LogsFolder, folder);
        await _context.NotificationService.ShowInfoAsync("Logs folder saved", folder);
    }

    private async void VisitWebsite_Click(object sender, RoutedEventArgs e)
    {
        await _context.ExternalLauncher.LaunchUriAsync(new Uri("https://rymate.co.uk/"));
    }

    private async void VisitGithub_Click(object sender, RoutedEventArgs e)
    {
        await _context.ExternalLauncher.LaunchUriAsync(new Uri("https://github.com/rymate1234/WinIRC"));
    }

    private async void JoinSupport_Click(object sender, RoutedEventArgs e)
    {
        await _context.ExternalLauncher.LaunchUriAsync(new Uri("https://overviewer.org/"));
    }

    private async void PrivacyPolicy_Click(object sender, RoutedEventArgs e)
    {
        await _context.ExternalLauncher.LaunchUriAsync(new Uri("http://rymate.co.uk/privacy-policy-windows-edition/"));
    }

    private static string GetVersionText()
    {
        try
        {
            var version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        catch
        {
            return typeof(SettingsPage).Assembly.GetName().Version?.ToString() ?? "development";
        }
    }
}
