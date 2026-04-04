using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using WinIRC.Core.Models;

namespace WinIRC.WinUI.Models;

public sealed class SessionTab : INotifyPropertyChanged
{
    private int _unreadCount;
    private bool _hasMention;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ChatSessionDescriptor Descriptor { get; set; } = new ChatSessionDescriptor();

    public string Header => Descriptor.Header;

    public string Subtitle => Descriptor.Subtitle;

    public UIElement? Content { get; set; }

    public bool CanDetach => Descriptor.Kind != ChatSessionKind.Settings && Descriptor.Kind != ChatSessionKind.Welcome;

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
            OnPropertyChanged(nameof(UnreadVisibility));
        }
    }

    public Visibility UnreadVisibility => UnreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;

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
        }
    }

    public string IdentityKey => Descriptor.Kind + "|" + Descriptor.ServerName + "|" + Descriptor.ChannelName + "|" + Descriptor.SourceUri;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
