using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using WinIRC.Core.Models;

namespace WinIRC.WinUI.Models;

public sealed class ChannelMessageGroupItem
{
    public ChannelMessageItem Parent { get; set; } = new();

    public ObservableCollection<ChannelMessageItem> Lines { get; } = new();

    public bool IsStatusGroup => Parent.IsStatus;

    public string Author => Parent.Author;

    public string Monogram => Parent.Monogram;

    public string TimestampText => Parent.TimestampText;

    public Visibility MessageVisibility => IsStatusGroup ? Visibility.Collapsed : Visibility.Visible;

    public Visibility StatusVisibility => IsStatusGroup ? Visibility.Visible : Visibility.Collapsed;

    public bool CanMerge(ChannelMessageItem message)
    {
        if (IsStatusGroup || message.IsStatus)
        {
            return Parent.Kind == IrcRuntimeMessageKind.JoinPart && message.Kind == IrcRuntimeMessageKind.JoinPart;
        }

        return string.Equals(Parent.Author, message.Author, StringComparison.Ordinal)
            && Parent.Kind == message.Kind
            && Parent.Timestamp.LocalDateTime.Date == message.Timestamp.LocalDateTime.Date
            && Parent.Timestamp.LocalDateTime.Hour == message.Timestamp.LocalDateTime.Hour;
    }
}
