using System.Text.Json.Serialization;

namespace WinIRC.Core.Models;

public sealed class WinIrcServer : IrcClientCore.IrcServer
{
    [JsonPropertyName("webSocket")]
    public bool WebSocket { get; set; }

    [JsonIgnore]
    public bool Invalid { get; set; }
}
