using Avalonia.Media;
using IrcClientCore;
using System.Text.RegularExpressions;
using Avalonia;
using WinIRC.Avalonia.Utilities;

namespace WinIRC.Avalonia.ViewModels;

public sealed partial class MessageViewModel
{
    public MessageViewModel(
        Message message,
        MessageViewModel? previous = null,
        bool modernChat = false,
        bool darkTheme = false,
        bool reducedPadding = false)
    {
        Time = string.IsNullOrWhiteSpace(message.Timestamp) ? message.Date.ToString("HH:mm") : message.Timestamp;
        User = FormatUser(message);
        Text = message.Text;
        Type = message.Type.ToString();
        IsMention = message.Mention;
        IsJoinPart = message.Type == MessageType.JoinPart;
        IsAction = message.Type == MessageType.Action;
        IsNotice = message.Type == MessageType.Notice;
        IsMotd = message.Type == MessageType.MOTD;
        SourceUser = message.User ?? "";
        SourceType = message.Type;
        SourceHour = message.Date.Hour;
        ShowHeader = !modernChat || previous == null || !CanGroupAfter(previous, message);
        FontWeight = IsMention ? FontWeight.SemiBold : FontWeight.Normal;
        FontStyle = IsAction ? FontStyle.Italic : FontStyle.Normal;
        Opacity = IsJoinPart ? 0.72 : 1.0;
        UserBrush = IsMention
            ? Brushes.IndianRed
            : new SolidColorBrush(NickColor.Generate(message.User ?? "", darkTheme));
        TextBrush = IsMention
            ? Brushes.IndianRed
            : message.Type is MessageType.Info or MessageType.JoinPart
                ? Brushes.Gray
                : null;
        LineMargin = reducedPadding ? new Thickness(0, 0) : new Thickness(0, 2);
        FirstLink = LinkRegex().Match(Text) is { Success: true } match ? match.Value : "";
    }

    public string Time { get; }
    public string User { get; }
    public string Text { get; }
    public string Type { get; }
    public bool IsMention { get; }
    public bool IsJoinPart { get; }
    public bool IsAction { get; }
    public bool IsNotice { get; }
    public bool IsMotd { get; }
    public bool ShowHeader { get; }
    public FontWeight FontWeight { get; }
    public FontStyle FontStyle { get; }
    public IBrush UserBrush { get; }
    public IBrush? TextBrush { get; }
    public Thickness LineMargin { get; }
    public double Opacity { get; }
    public string FirstLink { get; }
    public bool HasLink => !string.IsNullOrWhiteSpace(FirstLink);
    internal string SourceUser { get; }
    internal MessageType SourceType { get; }
    internal int SourceHour { get; }

    private static string FormatUser(Message message)
    {
        var user = message.User ?? "";
        if (user.Contains('*'))
        {
            return "*";
        }

        return message.Type switch
        {
            MessageType.Notice => $"->{user}<-",
            MessageType.Action => $"* {user}",
            MessageType.Info or MessageType.JoinPart => "",
            _ => user
        };
    }

    private static bool CanGroupAfter(MessageViewModel previous, Message message)
    {
        if (message.Type is MessageType.Info or MessageType.MOTD or MessageType.Notice)
        {
            return false;
        }

        if (previous.SourceType == MessageType.JoinPart && message.Type == MessageType.JoinPart)
        {
            return true;
        }

        return previous.SourceType == message.Type &&
            previous.SourceHour == message.Date.Hour &&
            string.Equals(previous.SourceUser, message.User ?? "", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"https?://[^\s<>'""]+", RegexOptions.IgnoreCase)]
    private static partial Regex LinkRegex();
}
