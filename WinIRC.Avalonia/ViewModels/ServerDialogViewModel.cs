using System.Collections.ObjectModel;
using WinIRC.Core.Configuration;
using WinIRC.Core.Models;
using WinIRC.Core.Persistence;

namespace WinIRC.Avalonia.ViewModels;

public sealed class ServerDialogViewModel : ObservableObject
{
    private readonly JsonSettingsStore settings;
    private WinIrcServer? selectedServer;
    private string name = "";
    private string hostname = "";
    private string port = "6667";
    private bool ssl;
    private bool webSocket;
    private string username = "";
    private string password = "";
    private string nickservPassword = "";
    private string channels = "";
    private string errorText = "";

    public ServerDialogViewModel(ServerStore serverStore, JsonSettingsStore settings)
    {
        ServerStore = serverStore;
        this.settings = settings;
        username = settings.GetString(ConfigKeys.DefaultUsername);
    }

    public ServerStore ServerStore { get; }
    public ObservableCollection<WinIrcServer> SavedServers => ServerStore.Servers;

    public WinIrcServer? SelectedServer
    {
        get => selectedServer;
        set
        {
            if (SetProperty(ref selectedServer, value) && value != null)
            {
                LoadServer(value);
            }
        }
    }

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    public string Hostname
    {
        get => hostname;
        set => SetProperty(ref hostname, value);
    }

    public string Port
    {
        get => port;
        set => SetProperty(ref port, value);
    }

    public bool Ssl
    {
        get => ssl;
        set => SetProperty(ref ssl, value);
    }

    public bool WebSocket
    {
        get => webSocket;
        set => SetProperty(ref webSocket, value);
    }

    public string Username
    {
        get => username;
        set => SetProperty(ref username, value);
    }

    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    public string NickservPassword
    {
        get => nickservPassword;
        set => SetProperty(ref nickservPassword, value);
    }

    public string Channels
    {
        get => channels;
        set => SetProperty(ref channels, value);
    }

    public string ErrorText
    {
        get => errorText;
        private set => SetProperty(ref errorText, value);
    }

    public WinIrcServer? BuildServer()
    {
        ErrorText = "";

        if (string.IsNullOrWhiteSpace(Hostname))
        {
            ErrorText = "Enter a hostname.";
            return null;
        }

        if (Uri.CheckHostName(Hostname) == UriHostNameType.Unknown)
        {
            ErrorText = "Hostname is not valid.";
            return null;
        }

        if (!int.TryParse(Port, out var parsedPort) || parsedPort < 1 || parsedPort > 65535)
        {
            ErrorText = "Port must be a number from 1 to 65535.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorText = "Enter a username.";
            return null;
        }

        if (Username.Contains(' '))
        {
            ErrorText = "Usernames cannot contain spaces.";
            return null;
        }

        var serverName = string.IsNullOrWhiteSpace(Name) ? Hostname : Name;
        settings.SetString(ConfigKeys.DefaultUsername, Username);

        return new WinIrcServer
        {
            Name = serverName,
            Hostname = Hostname,
            Port = parsedPort,
            Ssl = Ssl,
            WebSocket = WebSocket,
            Username = Username,
            Password = Password,
            NickservPassword = NickservPassword,
            Channels = Channels
        };
    }

    public async Task SaveAsync()
    {
        var server = BuildServer();
        if (server == null)
        {
            return;
        }

        await ServerStore.AddOrUpdateAsync(server);
        SelectedServer = server;
    }

    public async Task DeleteSelectedAsync()
    {
        if (SelectedServer == null)
        {
            return;
        }

        await ServerStore.DeleteAsync(SelectedServer.Name);
        SelectedServer = null;
        Clear();
    }

    public void NewServer()
    {
        SelectedServer = null;
        Clear();
    }

    public async Task ImportLegacyAsync(Stream stream)
    {
        var count = await ServerStore.ImportLegacyXmlAsync(stream);
        ErrorText = count == 1 ? "Imported 1 server." : $"Imported {count} servers.";
    }

    private void LoadServer(WinIrcServer server)
    {
        Name = server.Name;
        Hostname = server.Hostname;
        Port = server.Port.ToString();
        Ssl = server.Ssl;
        WebSocket = server.WebSocket;
        Username = server.Username;
        Password = server.Password;
        NickservPassword = server.NickservPassword;
        Channels = server.Channels;
        ErrorText = "";
    }

    private void Clear()
    {
        Name = "";
        Hostname = "";
        Port = "6667";
        Ssl = false;
        WebSocket = false;
        Password = "";
        NickservPassword = "";
        Channels = "";
        ErrorText = "";
    }
}
