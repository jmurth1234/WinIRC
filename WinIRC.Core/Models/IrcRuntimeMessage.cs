using System;

namespace WinIRC.Core.Models;

public sealed class IrcRuntimeMessage
{
    public string Author { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    public bool IsStatus { get; set; }

    public bool IsMention { get; set; }

    public IrcRuntimeMessageKind Kind { get; set; } = IrcRuntimeMessageKind.Normal;
}
