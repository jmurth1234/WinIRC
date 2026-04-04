using System;

namespace WinIRC.Core.Models;

public sealed class ChatSessionDescriptor
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public ChatSessionKind Kind { get; set; } = ChatSessionKind.Welcome;

    public string Header { get; set; } = "Welcome";

    public string Subtitle { get; set; } = string.Empty;

    public string ServerName { get; set; } = string.Empty;

    public string ChannelName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? SourceUri { get; set; }

    public bool IsDetached { get; set; }

    public ChatSessionDescriptor CloneDetached()
    {
        return new ChatSessionDescriptor
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = Kind,
            Header = Header,
            Subtitle = Subtitle,
            ServerName = ServerName,
            ChannelName = ChannelName,
            Description = Description,
            SourceUri = SourceUri,
            IsDetached = true
        };
    }
}
