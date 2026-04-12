using IrcClientCore;
using WinIRC.Core.Logging;

namespace WinIRC.Tests.Core;

public sealed class MessageLogWriterTests
{
    [Fact]
    public async Task WritesMessagesToDailyChannelLog()
    {
        var directory = CreateTempDirectory();
        using var writer = new MessageLogWriter("Example", directory);

        writer.Add("#winirc", new Message
        {
            Date = DateTime.Today.AddHours(12).AddMinutes(34),
            User = "alice",
            Text = "hello linux"
        });

        var path = Path.Combine(directory, "Example", "#winirc-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
        var content = await WaitForFileTextAsync(path);

        Assert.Contains("<alice> hello linux", content);
    }

    private static async Task<string> WaitForFileTextAsync(string path)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (File.Exists(path))
            {
                var content = await File.ReadAllTextAsync(path);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content;
                }
            }

            await Task.Delay(20);
        }

        return File.Exists(path) ? await File.ReadAllTextAsync(path) : "";
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winirc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
