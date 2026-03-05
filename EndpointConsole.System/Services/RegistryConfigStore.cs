using Microsoft.Win32;
using EndpointConsole.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EndpointConsole.WindowsSystem.Services;

public sealed class RegistryConfigStore(ILogger<RegistryConfigStore> logger) : IRegistryConfigStore
{
    private readonly ILogger<RegistryConfigStore> _logger = logger;

    public Task<IReadOnlyDictionary<string, string?>> ReadValuesAsync(
        string keyPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (hive, subKeyPath) = ParseKeyPath(keyPath);

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.OpenSubKey(subKeyPath, writable: false);

        if (key is null)
        {
            _logger.LogInformation("Registry key {KeyPath} not found.", keyPath);
            return Task.FromResult<IReadOnlyDictionary<string, string?>>(new Dictionary<string, string?>());
        }

        var values = key.GetValueNames()
            .ToDictionary(
                valueName => valueName,
                valueName => key.GetValue(valueName)?.ToString(),
                StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("Read {Count} registry values from {KeyPath}.", values.Count, keyPath);
        return Task.FromResult<IReadOnlyDictionary<string, string?>>(values);
    }

    public Task WriteValueAsync(
        string keyPath,
        string valueName,
        string? value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (hive, subKeyPath) = ParseKeyPath(keyPath);

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(subKeyPath, writable: true);

        if (key is null)
        {
            throw new InvalidOperationException($"Unable to create or open registry key: {keyPath}");
        }

        key.SetValue(valueName, value ?? string.Empty, RegistryValueKind.String);
        _logger.LogInformation("Wrote registry value {KeyPath}\\{ValueName}.", keyPath, valueName);
        return Task.CompletedTask;
    }

    private static (RegistryHive hive, string subKeyPath) ParseKeyPath(string keyPath)
    {
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            throw new ArgumentException("Registry key path is required.", nameof(keyPath));
        }

        var normalized = keyPath.Replace('/', '\\');
        var parts = normalized.Split('\\', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new ArgumentException("Registry key path is invalid.", nameof(keyPath));
        }

        var hive = parts[0].ToUpperInvariant() switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKU" or "HKEY_USERS" => RegistryHive.Users,
            _ => RegistryHive.CurrentUser
        };

        var subKeyPath = parts.Length > 1 ? parts[1] : string.Empty;
        return (hive, subKeyPath);
    }
}
