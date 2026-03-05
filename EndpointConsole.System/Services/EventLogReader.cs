using System.Diagnostics.Eventing.Reader;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using Microsoft.Extensions.Logging;
using CoreEventLogRecord = EndpointConsole.Core.Models.EventLogRecord;

namespace EndpointConsole.WindowsSystem.Services;

public sealed class EventLogReader(ILogger<EventLogReader> logger) : IEventLogReader
{
    private readonly ILogger<EventLogReader> _logger = logger;

    public Task<IReadOnlyList<CoreEventLogRecord>> GetRecentEntriesAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var count = Math.Clamp(maxCount, 1, 500);
        var records = new List<CoreEventLogRecord>(count);

        const string xPath = "*[System[(Level=2 or Level=3)]]";
        var query = new EventLogQuery("System", PathType.LogName, xPath)
        {
            ReverseDirection = true
        };

        using var reader = new System.Diagnostics.Eventing.Reader.EventLogReader(query);

        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var eventRecord = reader.ReadEvent();
            if (eventRecord is null)
            {
                break;
            }

            var message = TryReadMessage(eventRecord);
            records.Add(new CoreEventLogRecord
            {
                LogName = eventRecord.LogName ?? "System",
                Source = eventRecord.ProviderName ?? "Unknown",
                EventId = eventRecord.Id,
                Level = MapLevel(eventRecord.LevelDisplayName, eventRecord.Level),
                Message = message,
                TimestampUtc = eventRecord.TimeCreated?.ToUniversalTime() ?? DateTimeOffset.UtcNow
            });
        }

        _logger.LogInformation("Read {Count} event log records.", records.Count);
        return Task.FromResult<IReadOnlyList<CoreEventLogRecord>>(records);
    }

    private static string TryReadMessage(EventRecord eventRecord)
    {
        try
        {
            var description = eventRecord.FormatDescription();
            return string.IsNullOrWhiteSpace(description) ? "(No description)" : description;
        }
        catch
        {
            return "(Description unavailable)";
        }
    }

    private static string MapLevel(string? levelDisplayName, byte? level)
    {
        if (!string.IsNullOrWhiteSpace(levelDisplayName))
        {
            return levelDisplayName;
        }

        return level switch
        {
            1 => "Critical",
            2 => "Error",
            3 => "Warning",
            4 => "Information",
            5 => "Verbose",
            _ => "Unknown"
        };
    }
}
