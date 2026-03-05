namespace EndpointConsole.Core.Interfaces;

public interface IRegistryConfigStore
{
    Task<IReadOnlyDictionary<string, string?>> ReadValuesAsync(
        string keyPath,
        CancellationToken cancellationToken = default);

    Task WriteValueAsync(
        string keyPath,
        string valueName,
        string? value,
        CancellationToken cancellationToken = default);
}
