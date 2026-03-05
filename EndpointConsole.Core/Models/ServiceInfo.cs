namespace EndpointConsole.Core.Models;

public sealed record ServiceInfo
{
    public string ServiceName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string StartType { get; init; } = string.Empty;

    public bool CanStop { get; init; }

    public bool CanPauseAndContinue { get; init; }
}
