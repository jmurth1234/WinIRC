using System.Text;
using WinIRC.Core.Models;
using WinIRC.Core.Persistence;

namespace WinIRC.Tests.Core;

public sealed class ServerStoreTests
{
    [Fact]
    public async Task AddUpdateSaveAndLoadRoundTripsServers()
    {
        var directory = CreateTempDirectory();
        var store = new ServerStore(directory);

        await store.AddOrUpdateAsync(new WinIrcServer
        {
            Name = "Libera",
            Hostname = "irc.libera.chat",
            Port = 6697,
            Ssl = true,
            Username = "tester",
            Channels = "#winirc"
        });

        var reloaded = new ServerStore(directory);
        await reloaded.LoadAsync();

        var server = Assert.Single(reloaded.Servers);
        Assert.Equal("Libera", server.Name);
        Assert.Equal("irc.libera.chat", server.Hostname);
        Assert.Equal(6697, server.Port);
        Assert.True(server.Ssl);
        Assert.Equal("#winirc", server.Channels);
    }

    [Fact]
    public async Task ImportLegacyXmlReadsCurrentUwpServerShape()
    {
        var directory = CreateTempDirectory();
        var store = new ServerStore(directory);
        var xml = """
            <ArrayOfIrcServer>
              <IrcServer>
                <name>Example</name>
                <hostname>irc.example.net</hostname>
                <port>6667</port>
                <ssl>false</ssl>
                <webSocket>true</webSocket>
                <username>tester</username>
                <password>serverpass</password>
                <nickservPassword>nickpass</nickservPassword>
                <channels>#one,#two</channels>
              </IrcServer>
            </ArrayOfIrcServer>
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var count = await store.ImportLegacyXmlAsync(stream);

        Assert.Equal(1, count);
        var server = Assert.Single(store.Servers);
        Assert.Equal("Example", server.Name);
        Assert.Equal("irc.example.net", server.Hostname);
        Assert.True(server.WebSocket);
        Assert.Equal("nickpass", server.NickservPassword);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winirc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
