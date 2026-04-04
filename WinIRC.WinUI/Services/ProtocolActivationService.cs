using System;
using WinIRC.Core.Interfaces;

namespace WinIRC.WinUI.Services;

public sealed class ProtocolActivationService : IProtocolActivationService
{
    public Uri? PendingProtocolUri { get; private set; }

    public Uri? ConsumePendingProtocolUri()
    {
        var value = PendingProtocolUri;
        PendingProtocolUri = null;
        return value;
    }

    public void SetPendingProtocolUri(Uri uri)
    {
        PendingProtocolUri = uri;
    }
}
