using WinIRC.Core.Models;

namespace WinIRC.Avalonia.ViewModels;

public sealed class ChannelListEntryViewModel
{
    public ChannelListEntryViewModel(ChannelListEntry entry)
    {
        Channel = entry.Channel;
        Topic = entry.Topic;
        Users = entry.Users;
    }

    public string Channel { get; }
    public string Topic { get; }
    public int Users { get; }
}
