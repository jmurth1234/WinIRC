namespace WinIRC.Core.Models;

public sealed class SavedServer
{
    public string Name { get; set; } = string.Empty;

    public string Hostname { get; set; } = string.Empty;

    public int Port { get; set; } = 6667;

    public bool Ssl { get; set; }

    public bool UseWebSocket { get; set; }

    public bool AutoConnect { get; set; }

    public bool Invalid { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string NickservPassword { get; set; } = string.Empty;

    public string Channels { get; set; } = string.Empty;

    public string DisplayEndpoint => Port > 0 ? Hostname + ":" + Port : Hostname;
}
