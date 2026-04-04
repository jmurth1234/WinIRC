using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinIRC.Core.Models;

namespace WinIRC.WinUI.Models;

public sealed class ChannelMessageItem
{
    public string Author { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    public bool IsStatus { get; set; }

    public bool IsMention { get; set; }

    public IrcRuntimeMessageKind Kind { get; set; } = IrcRuntimeMessageKind.Normal;

    public string TimestampText => Timestamp.ToLocalTime().ToString("HH:mm");

    public string Monogram => Author.Length > 0 ? Author[..1].ToUpperInvariant() : "?";

    public Brush? AuthorBrush { get; set; }

    public Brush? MonogramBackgroundBrush { get; set; }

    public Visibility MessageVisibility => IsStatus ? Visibility.Collapsed : Visibility.Visible;

    public Visibility StatusVisibility => IsStatus ? Visibility.Visible : Visibility.Collapsed;
}
