using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace EndpointConsole.Wpf.Services;

public sealed class OperationExecutor(ILogger<OperationExecutor> logger) : IOperationExecutor
{
    private readonly ILogger<OperationExecutor> _logger = logger;

    public async Task<OperationExecutionResult> ExecuteAsync(
        string operationName,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        using var correlationScope = LogContext.PushProperty("CorrelationId", correlationId);
        using var loggerScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["OperationName"] = operationName
        });

        _logger.LogInformation("Operation {OperationName} started.", operationName);

        try
        {
            await operation(cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Operation {OperationName} completed in {DurationMs} ms.",
                operationName,
                stopwatch.ElapsedMilliseconds);

            return new OperationExecutionResult(correlationId, stopwatch.Elapsed, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Operation {OperationName} cancelled after {DurationMs} ms.",
                operationName,
                stopwatch.ElapsedMilliseconds);

            return new OperationExecutionResult(correlationId, stopwatch.Elapsed, false, "Operation cancelled.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Operation {OperationName} failed after {DurationMs} ms.",
                operationName,
                stopwatch.ElapsedMilliseconds);

            return new OperationExecutionResult(correlationId, stopwatch.Elapsed, false, ex.Message);
        }
    }
}
