using System.Threading.Tasks;
using WinIRC.Core.Models;

namespace WinIRC.Core.Interfaces;

public interface IIrcRuntimeService
{
    IrcRuntimeSessionState GetSession(SavedServer server, string channelName);

    IrcRuntimeSessionState? TryGetExistingSession(SavedServer server, string channelName);

    IrcRuntimeTargetInfo? TryGetTargetInfo(SavedServer server, string channelName);

    Task EnsureConnectedAsync(SavedServer server);

    Task OpenChannelAsync(SavedServer server, string channelName);

    Task SendAsync(SavedServer server, string channelName, string text);
}
