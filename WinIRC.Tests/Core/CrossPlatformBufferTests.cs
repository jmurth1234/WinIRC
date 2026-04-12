using IrcClientCore;
using WinIRC.Core.Irc;

namespace WinIRC.Tests.Core;

public sealed class CrossPlatformBufferTests
{
    [Fact]
    public void AddTrimsOldestMessagesAtMaxSize()
    {
        var buffer = new CrossPlatformBuffer(maxMessages: 2);

        buffer.Add(new Message { Text = "one" });
        buffer.Add(new Message { Text = "two" });
        buffer.Add(new Message { Text = "three" });

        Assert.Equal(new[] { "two", "three" }, buffer.Collection.Select(message => message.Text));
    }
}
