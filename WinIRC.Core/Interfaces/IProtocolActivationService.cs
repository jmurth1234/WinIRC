using System;

namespace WinIRC.Core.Interfaces;

public interface IProtocolActivationService
{
    Uri? PendingProtocolUri { get; }

    void SetPendingProtocolUri(Uri uri);

    Uri? ConsumePendingProtocolUri();
}
