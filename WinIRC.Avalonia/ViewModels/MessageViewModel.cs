using Avalonia.Media;
using IrcClientCore;
using System.Text.RegularExpressions;

namespace WinIRC.Avalonia.ViewModels;

public sealed partial class MessageViewModel
{
    public MessageViewModel(Message message)
    {
        Time = string.IsNullOrWhiteSpace(message.Timestamp) ? message.Date.ToString("HH:mm") : message.Timestamp;
        User = message.User;
        Text = message.Text;
        Type = message.Type.ToString();
        IsMention = message.Mention;
        IsJoinPart = message.Type == MessageType.JoinPart;
        FontWeight = IsMention ? FontWeight.SemiBold : FontWeight.Normal;
        Opacity = IsJoinPart ? 0.72 : 1.0;
        FirstLink = LinkRegex().Match(Text) is { Success: true } match ? match.Value : "";
    }

    public string Time { get; }
    public string User { get; }
    public string Text { get; }
    public string Type { get; }
    public bool IsMention { get; }
    public bool IsJoinPart { get; }
    public FontWeight FontWeight { get; }
    public double Opacity { get; }
    public string FirstLink { get; }
    public bool HasLink => !string.IsNullOrWhiteSpace(FirstLink);

    [GeneratedRegex(@"https?://[^\s<>'""]+", RegexOptions.IgnoreCase)]
    private static partial Regex LinkRegex();
}
