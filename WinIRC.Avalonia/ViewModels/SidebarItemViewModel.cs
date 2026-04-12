using WinIRC.Core.Irc;

namespace WinIRC.Avalonia.ViewModels;

public sealed class SidebarItemViewModel : ObservableObject
{
    private int unreadCount;
    private bool hasMentions;

    public SidebarItemViewModel(IrcSession session, string channel)
    {
        Session = session;
        Channel = channel;
    }

    public IrcSession Session { get; }
    public string Channel { get; }
    public bool IsServer => Channel == IrcSession.ServerBufferName;
    public bool IsChannel => !IsServer;
    public string DisplayName
    {
        get
        {
            var name = IsServer ? Session.Server.Name : "  " + Channel;
            if (UnreadCount > 0)
            {
                name += $" ({UnreadCount})";
            }

            return HasMentions ? name + " *" : name;
        }
    }

    public int UnreadCount
    {
        get => unreadCount;
        set
        {
            if (SetProperty(ref unreadCount, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public bool HasMentions
    {
        get => hasMentions;
        set
        {
            if (SetProperty(ref hasMentions, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }
}
