using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using WinIRC.Core.Interfaces;

namespace WinIRC.WinUI.Services;

public sealed class DesktopNotificationService : INotificationService
{
    private bool _initialized;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return Task.CompletedTask;
        }

        try
        {
            AppNotificationManager.Default.Register();
            _initialized = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        return Task.CompletedTask;
    }

    public Task ShowInfoAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        return Task.CompletedTask;
    }
}
