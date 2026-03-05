using EndpointConsole.Core.Models;

namespace EndpointConsole.Core.Interfaces;

public interface ISessionEnumerator
{
    Task<IReadOnlyList<UserSessionInfo>> EnumerateSessionsAsync(
        CancellationToken cancellationToken = default);
}
