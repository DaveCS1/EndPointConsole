namespace EndpointConsole.Core.Models;

public sealed record UserSessionInfo
{
    public int SessionId { get; init; }

    public string SessionName { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string Domain { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;
}
