using System.IO.Compression;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using EndpointConsole.WindowsSystem.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EndpointConsole.Tests.System;

public class DiagnosticsBundleBuilderTests
{
    [TestCase(DiagnosticsBuildMode.Baseline)]
    [TestCase(DiagnosticsBuildMode.Optimized)]
    public async Task BuildAsync_CreatesBundleWithExpectedArtifacts(DiagnosticsBuildMode mode)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "EndpointConsole.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var builder = CreateBuilder();
            var manifest = await builder.BuildAsync(tempDirectory, mode, CancellationToken.None);

            Assert.That(File.Exists(manifest.OutputZipPath), Is.True, "Zip file was not created.");
            Assert.That(manifest.IncludedFiles, Does.Contain("machine-snapshot.json"));
            Assert.That(manifest.IncludedFiles, Does.Contain("services.json"));
            Assert.That(manifest.IncludedFiles, Does.Contain("event-log.json"));
            Assert.That(manifest.IncludedFiles, Does.Contain("registry.json"));

            var manifestPath = Path.Combine(
                tempDirectory,
                $"{Path.GetFileNameWithoutExtension(manifest.OutputZipPath)}-manifest.json");
            Assert.That(File.Exists(manifestPath), Is.True, "Manifest file was not created.");

            using var zipArchive = ZipFile.OpenRead(manifest.OutputZipPath);
            var entryNames = zipArchive.Entries.Select(entry => entry.FullName).ToArray();
            Assert.That(entryNames.Any(name => name.EndsWith("machine-snapshot.json", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(entryNames.Any(name => name.EndsWith("services.json", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(entryNames.Any(name => name.EndsWith("event-log.json", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(entryNames.Any(name => name.EndsWith("registry.json", StringComparison.OrdinalIgnoreCase)), Is.True);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static DiagnosticsBundleBuilder CreateBuilder()
    {
        var snapshotProvider = new Mock<IMachineSnapshotProvider>();
        snapshotProvider
            .Setup(provider => provider.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MachineSnapshot
            {
                MachineName = "test-machine",
                OsDescription = "Windows Test",
                OsBuild = "26000",
                Uptime = TimeSpan.FromHours(5),
                SystemDriveFreeGb = 128,
                SystemDriveTotalGb = 512
            });

        var serviceManager = new Mock<IServiceManager>();
        serviceManager
            .Setup(manager => manager.ListServicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ServiceInfo
                {
                    ServiceName = "Spooler",
                    DisplayName = "Print Spooler",
                    Status = "Running",
                    StartType = "Automatic"
                }
            ]);

        var eventLogReader = new Mock<IEventLogReader>();
        eventLogReader
            .Setup(reader => reader.GetRecentEntriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new EventLogRecord
                {
                    EventId = 1234,
                    Level = "Warning",
                    Source = "UnitTest",
                    Message = "Sample warning"
                }
            ]);

        var registryStore = new Mock<IRegistryConfigStore>();
        registryStore
            .Setup(store => store.ReadValuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?> { ["SampleMode"] = "Enabled" });

        return new DiagnosticsBundleBuilder(
            new NullLogger<DiagnosticsBundleBuilder>(),
            snapshotProvider.Object,
            serviceManager.Object,
            eventLogReader.Object,
            registryStore.Object);
    }
}
