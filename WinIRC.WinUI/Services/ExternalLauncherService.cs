using System;
using System.Threading.Tasks;
using WinIRC.Core.Interfaces;

namespace WinIRC.WinUI.Services;

public sealed class ExternalLauncherService : IExternalLauncher
{
    public async Task LaunchUriAsync(Uri uri)
    {
        await Windows.System.Launcher.LaunchUriAsync(uri);
    }
}
