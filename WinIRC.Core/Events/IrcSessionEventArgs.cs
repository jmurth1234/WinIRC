using WinIRC.Core.Models;

namespace WinIRC.Core.Events;

public enum IrcSessionEventKind
{
    StatusChanged,
    ChannelAdded,
    ChannelRemoved,
    MessageAdded,
    UsersUpdated,
    TopicChanged,
    ChannelListReceived,
    Disconnected
}

public sealed class IrcSessionEventArgs : EventArgs
{
    public IrcSessionEventArgs(
        IrcSessionEventKind kind,
        string? channel = null,
        IrcClientCore.Message? message = null,
        IReadOnlyList<string>? users = null,
        IReadOnlyList<ChannelListEntry>? channelList = null,
        string? text = null,
        Exception? exception = null)
    {
        Kind = kind;
        Channel = channel;
        Message = message;
        Users = users;
        ChannelList = channelList;
        Text = text;
        Exception = exception;
    }

    public IrcSessionEventKind Kind { get; }
    public string? Channel { get; }
    public IrcClientCore.Message? Message { get; }
    public IReadOnlyList<string>? Users { get; }
    public IReadOnlyList<ChannelListEntry>? ChannelList { get; }
    public string? Text { get; }
    public Exception? Exception { get; }
}
