using System.Buffers;
using System.IO.Compression;
using System.Text.Json;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using EndpointConsole.WindowsSystem.Serialization;
using Microsoft.Extensions.Logging;

namespace EndpointConsole.WindowsSystem.Services;

public sealed class DiagnosticsBundleBuilder(
    ILogger<DiagnosticsBundleBuilder> logger,
    IMachineSnapshotProvider machineSnapshotProvider,
    IServiceManager serviceManager,
    IEventLogReader eventLogReader,
    IRegistryConfigStore registryConfigStore) : IDiagnosticsBundleBuilder
{
    private static readonly JsonSerializerOptions BaselineJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<DiagnosticsBundleBuilder> _logger = logger;
    private readonly IMachineSnapshotProvider _machineSnapshotProvider = machineSnapshotProvider;
    private readonly IServiceManager _serviceManager = serviceManager;
    private readonly IEventLogReader _eventLogReader = eventLogReader;
    private readonly IRegistryConfigStore _registryConfigStore = registryConfigStore;

    public Task<DiagnosticsBundleManifest> BuildAsync(
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        return BuildAsync(outputDirectory, DiagnosticsBuildMode.Optimized, cancellationToken);
    }

    public async Task<DiagnosticsBundleManifest> BuildAsync(
        string outputDirectory,
        DiagnosticsBuildMode buildMode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
        }

        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(outputDirectory);

        var bundleId = Guid.NewGuid().ToString("N")[..8];
        var bundlePrefix = $"bundle-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{bundleId}";
        var zipPath = Path.Combine(outputDirectory, $"{bundlePrefix}.zip");
        var manifestPath = Path.Combine(outputDirectory, $"{bundlePrefix}-manifest.json");

        var data = await CollectBundleDataAsync(cancellationToken);

        _logger.LogInformation("Building diagnostics bundle using {BuildMode} mode.", buildMode);
        return buildMode switch
        {
            DiagnosticsBuildMode.Baseline => await BuildBaselineAsync(
                outputDirectory,
                bundlePrefix,
                zipPath,
                manifestPath,
                data,
                cancellationToken),
            _ => await BuildOptimizedAsync(
                zipPath,
                manifestPath,
                data,
                cancellationToken)
        };
    }

    private async Task<BundleData> CollectBundleDataAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _machineSnapshotProvider.GetSnapshotAsync(cancellationToken);
        var services = (await _serviceManager.ListServicesAsync(cancellationToken)).ToList();
        var events = (await _eventLogReader.GetRecentEntriesAsync(200, cancellationToken)).ToList();

        const string registryKeyPath = @"HKCU\SOFTWARE\EndpointConsole";
        var registryValues = await _registryConfigStore.ReadValuesAsync(registryKeyPath, cancellationToken);
        var registrySnapshot = new RegistrySnapshotData(registryKeyPath, registryValues);

        var logFiles = GetLogFiles();
        return new BundleData(snapshot, services, events, registrySnapshot, logFiles);
    }

    private async Task<DiagnosticsBundleManifest> BuildBaselineAsync(
        string outputDirectory,
        string bundlePrefix,
        string zipPath,
        string manifestPath,
        BundleData data,
        CancellationToken cancellationToken)
    {
        var stagingDirectory = Path.Combine(outputDirectory, bundlePrefix);
        if (Directory.Exists(stagingDirectory))
        {
            Directory.Delete(stagingDirectory, recursive: true);
        }

        Directory.CreateDirectory(stagingDirectory);
        var includedFiles = new List<string>();

        try
        {
            await WriteJsonFileBaselineAsync(
                stagingDirectory,
                "machine-snapshot.json",
                data.Snapshot,
                includedFiles,
                cancellationToken);

            await WriteJsonFileBaselineAsync(
                stagingDirectory,
                "services.json",
                data.Services,
                includedFiles,
                cancellationToken);

            await WriteJsonFileBaselineAsync(
                stagingDirectory,
                "event-log.json",
                data.Events,
                includedFiles,
                cancellationToken);

            await WriteJsonFileBaselineAsync(
                stagingDirectory,
                "registry.json",
                data.RegistrySnapshot,
                includedFiles,
                cancellationToken);

            CopyLogsToStaging(stagingDirectory, data.LogFiles, includedFiles, cancellationToken);

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(stagingDirectory, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            var manifest = CreateManifest(data.Snapshot.MachineName, zipPath, includedFiles);
            await WriteManifestBaselineAsync(manifestPath, manifest, cancellationToken);
            return manifest;
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }
    }

    private async Task<DiagnosticsBundleManifest> BuildOptimizedAsync(
        string zipPath,
        string manifestPath,
        BundleData data,
        CancellationToken cancellationToken)
    {
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        var includedFiles = new List<string>();
        await using (var zipStream = File.Create(zipPath))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            await WriteJsonEntryOptimizedAsync(
                archive,
                "machine-snapshot.json",
                data.Snapshot,
                DiagnosticsJsonContext.Default.MachineSnapshot,
                includedFiles,
                cancellationToken);

            await WriteJsonEntryOptimizedAsync(
                archive,
                "services.json",
                data.Services,
                DiagnosticsJsonContext.Default.ListServiceInfo,
                includedFiles,
                cancellationToken);

            await WriteJsonEntryOptimizedAsync(
                archive,
                "event-log.json",
                data.Events,
                DiagnosticsJsonContext.Default.ListEventLogRecord,
                includedFiles,
                cancellationToken);

            await WriteJsonEntryOptimizedAsync(
                archive,
                "registry.json",
                data.RegistrySnapshot,
                DiagnosticsJsonContext.Default.RegistrySnapshotData,
                includedFiles,
                cancellationToken);

            await CopyLogsToArchiveAsync(archive, data.LogFiles, includedFiles, cancellationToken);
        }

        var manifest = CreateManifest(data.Snapshot.MachineName, zipPath, includedFiles);
        await WriteManifestOptimizedAsync(manifestPath, manifest, cancellationToken);
        return manifest;
    }

    private static DiagnosticsBundleManifest CreateManifest(
        string machineName,
        string zipPath,
        ICollection<string> includedFiles)
    {
        return new DiagnosticsBundleManifest
        {
            CorrelationId = Guid.NewGuid(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            MachineName = machineName,
            OutputZipPath = zipPath,
            IncludedFiles = includedFiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    private static async Task WriteJsonFileBaselineAsync<T>(
        string stagingDirectory,
        string fileName,
        T payload,
        ICollection<string> includedFiles,
        CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(stagingDirectory, fileName);
        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, payload, BaselineJsonOptions, cancellationToken);
        includedFiles.Add(fileName);
    }

    private static async Task WriteManifestBaselineAsync(
        string manifestPath,
        DiagnosticsBundleManifest manifest,
        CancellationToken cancellationToken)
    {
        await using var fileStream = File.Create(manifestPath);
        await JsonSerializer.SerializeAsync(fileStream, manifest, BaselineJsonOptions, cancellationToken);
    }

    private static async Task WriteManifestOptimizedAsync(
        string manifestPath,
        DiagnosticsBundleManifest manifest,
        CancellationToken cancellationToken)
    {
        await using var fileStream = File.Create(manifestPath);
        await JsonSerializer.SerializeAsync(
            fileStream,
            manifest,
            DiagnosticsJsonContext.Default.DiagnosticsBundleManifest,
            cancellationToken);
    }

    private static async Task WriteJsonEntryOptimizedAsync<T>(
        ZipArchive archive,
        string entryName,
        T payload,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo,
        ICollection<string> includedFiles,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        await JsonSerializer.SerializeAsync(entryStream, payload, jsonTypeInfo, cancellationToken);
        includedFiles.Add(entryName);
    }

    private static void CopyLogsToStaging(
        string stagingDirectory,
        IReadOnlyList<string> logFiles,
        ICollection<string> includedFiles,
        CancellationToken cancellationToken)
    {
        if (logFiles.Count == 0)
        {
            return;
        }

        var destinationLogDirectory = Path.Combine(stagingDirectory, "logs");
        Directory.CreateDirectory(destinationLogDirectory);

        foreach (var logPath in logFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(logPath);
            var destinationPath = Path.Combine(destinationLogDirectory, fileName);
            File.Copy(logPath, destinationPath, overwrite: true);
            includedFiles.Add(Path.Combine("logs", fileName));
        }
    }

    private static async Task CopyLogsToArchiveAsync(
        ZipArchive archive,
        IReadOnlyList<string> logFiles,
        ICollection<string> includedFiles,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            foreach (var logPath in logFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileName = Path.GetFileName(logPath);
                var entryName = Path.Combine("logs", fileName).Replace('\\', '/');
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                await using var sourceStream = File.OpenRead(logPath);
                await using var destinationStream = entry.Open();
                int bytesRead;
                while ((bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }

                includedFiles.Add(entryName);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static IReadOnlyList<string> GetLogFiles()
    {
        var sourceLogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EndpointConsole",
            "Logs");

        if (!Directory.Exists(sourceLogDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(sourceLogDirectory, "*.log")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(10)
            .ToArray();
    }

    private sealed record BundleData(
        MachineSnapshot Snapshot,
        List<ServiceInfo> Services,
        List<EventLogRecord> Events,
        RegistrySnapshotData RegistrySnapshot,
        IReadOnlyList<string> LogFiles);
}
