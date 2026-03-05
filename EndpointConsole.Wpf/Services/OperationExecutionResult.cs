namespace EndpointConsole.Wpf.Services;

public sealed record OperationExecutionResult(
    Guid CorrelationId,
    TimeSpan Duration,
    bool Succeeded,
    string? ErrorMessage = null);
