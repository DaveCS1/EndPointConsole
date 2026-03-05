using System.Diagnostics;
using EndpointConsole.Wpf.Services;

namespace EndpointConsole.Tests.Helpers;

internal sealed class InlineOperationExecutor : IOperationExecutor
{
    public async Task<OperationExecutionResult> ExecuteAsync(
        string operationName,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        _ = operationName;
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid();

        try
        {
            await operation(cancellationToken);
            stopwatch.Stop();
            return new OperationExecutionResult(correlationId, stopwatch.Elapsed, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new OperationExecutionResult(correlationId, stopwatch.Elapsed, false, "Operation cancelled.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new OperationExecutionResult(correlationId, stopwatch.Elapsed, false, ex.Message);
        }
    }
}
