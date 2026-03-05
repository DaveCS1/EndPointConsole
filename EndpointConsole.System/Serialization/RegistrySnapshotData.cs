namespace EndpointConsole.WindowsSystem.Serialization;

internal sealed record RegistrySnapshotData(
    string KeyPath,
    IReadOnlyDictionary<string, string?> Values);
