using System.Threading.Tasks;
using WinIRC.Core.Interfaces;

namespace WinIRC.WinUI.Services;

public sealed class ShareService : IShareService
{
    private readonly INotificationService _notificationService;

    public ShareService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task ShareTextAsync(string title, string text)
    {
        await _notificationService.ShowInfoAsync(title, "System share UI will be wired in a later migration slice.");
    }
}
