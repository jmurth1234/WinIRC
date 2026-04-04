using System;
using System.Threading.Tasks;

namespace WinIRC.Core.Interfaces;

public interface IExternalLauncher
{
    Task LaunchUriAsync(Uri uri);
}
