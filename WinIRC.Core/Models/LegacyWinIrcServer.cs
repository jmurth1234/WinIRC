using System;
using System.Xml.Serialization;

namespace WinIRC.Core.Models;

[Serializable]
public sealed class LegacyWinIrcServer
{
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("Hostname")]
    public string Hostname { get; set; } = string.Empty;

    [XmlElement("Port")]
    public int Port { get; set; } = 6667;

    [XmlElement("Ssl")]
    public bool Ssl { get; set; }

    [XmlElement("Username")]
    public string Username { get; set; } = string.Empty;

    [XmlElement("Password")]
    public string Password { get; set; } = string.Empty;

    [XmlElement("NickservPassword")]
    public string NickservPassword { get; set; } = string.Empty;

    [XmlElement("Channels")]
    public string Channels { get; set; } = string.Empty;

    [XmlElement("webSocket")]
    public bool WebSocket { get; set; }

    [XmlElement("invalid")]
    public bool Invalid { get; set; }

    public SavedServer ToSavedServer()
    {
        return new SavedServer
        {
            Name = Name,
            Hostname = Hostname,
            Port = Port,
            Ssl = Ssl,
            Username = Username,
            Password = Password,
            NickservPassword = NickservPassword,
            Channels = Channels,
            UseWebSocket = WebSocket,
            Invalid = Invalid
        };
    }
}
