using System.Threading;
using System.Threading.Tasks;

namespace WinIRC.Core.Interfaces;

public interface INotificationService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task ShowInfoAsync(string title, string message, CancellationToken cancellationToken = default);
}
