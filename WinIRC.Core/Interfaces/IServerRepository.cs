using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinIRC.Core.Models;

namespace WinIRC.Core.Interfaces;

public interface IServerRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedServer>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IEnumerable<SavedServer> servers, CancellationToken cancellationToken = default);
}
