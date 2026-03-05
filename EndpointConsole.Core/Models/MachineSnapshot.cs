namespace EndpointConsole.Core.Models;

public sealed record MachineSnapshot
{
    public string MachineName { get; init; } = string.Empty;

    public string OsDescription { get; init; } = string.Empty;

    public string OsVersion { get; init; } = string.Empty;

    public string OsBuild { get; init; } = string.Empty;

    public bool IsDomainJoined { get; init; }

    public TimeSpan Uptime { get; init; }

    public double SystemDriveTotalGb { get; init; }

    public double SystemDriveFreeGb { get; init; }

    public DateTimeOffset CollectedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
