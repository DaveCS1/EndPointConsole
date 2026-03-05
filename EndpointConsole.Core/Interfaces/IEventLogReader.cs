using EndpointConsole.Core.Models;

namespace EndpointConsole.Core.Interfaces;

public interface IEventLogReader
{
    Task<IReadOnlyList<EventLogRecord>> GetRecentEntriesAsync(
        int maxCount,
        CancellationToken cancellationToken = default);
}
