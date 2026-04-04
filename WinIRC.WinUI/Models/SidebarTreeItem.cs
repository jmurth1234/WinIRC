using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using WinIRC.Core.Models;

namespace WinIRC.WinUI.Models;

public sealed class SidebarTreeItem : INotifyPropertyChanged
{
    private SavedServer _server = new();
    private WorkspaceTarget? _target;
    private string _secondaryText = string.Empty;
    private int _unreadCount;
    private bool _hasMention;
    private bool _isExpanded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SavedServer Server
    {
        get => _server;
        set
        {
            if (ReferenceEquals(_server, value))
            {
                return;
            }

            _server = value;
            OnPropertyChanged();
        }
    }

    public WorkspaceTarget? Target
    {
        get => _target;
        set
        {
            if (ReferenceEquals(_target, value))
            {
                return;
            }

            _target = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsServer));
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string SecondaryText
    {
        get => _secondaryText;
        set
        {
            if (string.Equals(_secondaryText, value, System.StringComparison.Ordinal))
            {
                return;
            }

            _secondaryText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SecondaryVisibility));
        }
    }

    public bool IsServer => Target == null;

    public string DisplayName => IsServer ? Server.Name : Target!.DisplayName;

    public Visibility SecondaryVisibility => string.IsNullOrWhiteSpace(SecondaryText)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public ObservableCollection<SidebarTreeItem> Children { get; } = new();

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public int UnreadCount
    {
        get => _unreadCount;
        set
        {
            if (_unreadCount == value)
            {
                return;
            }

            _unreadCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UnreadBadgeText));
            OnPropertyChanged(nameof(UnreadVisibility));
            OnPropertyChanged(nameof(UnreadOnlyVisibility));
            OnPropertyChanged(nameof(MentionVisibility));
        }
    }

    public bool HasMention
    {
        get => _hasMention;
        set
        {
            if (_hasMention == value)
            {
                return;
            }

            _hasMention = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UnreadOnlyVisibility));
            OnPropertyChanged(nameof(MentionVisibility));
        }
    }

    public string UnreadBadgeText => UnreadCount > 99 ? "99+" : UnreadCount.ToString();

    public Visibility UnreadVisibility => UnreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility UnreadOnlyVisibility => UnreadCount > 0 && !HasMention
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility MentionVisibility => UnreadCount > 0 && HasMention
        ? Visibility.Visible
        : Visibility.Collapsed;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
