namespace EndpointConsole.Wpf.Services;

public interface IOperationExecutor
{
    Task<OperationExecutionResult> ExecuteAsync(
        string operationName,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
