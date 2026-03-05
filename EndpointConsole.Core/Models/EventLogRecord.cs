namespace EndpointConsole.Core.Models;

public sealed record EventLogRecord
{
    public string LogName { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public int EventId { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}
