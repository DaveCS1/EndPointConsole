namespace EndpointConsole.Core.Models;

public sealed record DiagnosticsBundleManifest
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string MachineName { get; init; } = string.Empty;

    public string OutputZipPath { get; init; } = string.Empty;

    public IReadOnlyList<string> IncludedFiles { get; init; } = Array.Empty<string>();
}
