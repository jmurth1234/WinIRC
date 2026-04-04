namespace WinIRC.Core.Models;

public sealed class IrcRuntimeTargetInfo
{
    public bool IsConnected { get; set; }

    public string ConnectionState { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }

    public string Topic { get; set; } = string.Empty;
}
