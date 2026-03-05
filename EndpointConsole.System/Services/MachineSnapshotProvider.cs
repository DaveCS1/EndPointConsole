using System.Runtime.InteropServices;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using Microsoft.Extensions.Logging;

namespace EndpointConsole.WindowsSystem.Services;

public sealed class MachineSnapshotProvider(ILogger<MachineSnapshotProvider> logger) : IMachineSnapshotProvider
{
    private readonly ILogger<MachineSnapshotProvider> _logger = logger;

    public Task<MachineSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var osVersion = Environment.OSVersion.Version;
        var systemDrive = GetSystemDriveInfo();

        var snapshot = new MachineSnapshot
        {
            MachineName = Environment.MachineName,
            OsDescription = RuntimeInformation.OSDescription,
            OsVersion = osVersion.ToString(),
            OsBuild = osVersion.Build.ToString(),
            IsDomainJoined = IsDomainJoined(),
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            SystemDriveTotalGb = systemDrive.totalGb,
            SystemDriveFreeGb = systemDrive.freeGb,
            CollectedAtUtc = DateTimeOffset.UtcNow
        };

        _logger.LogInformation(
            "Snapshot collected for {MachineName}. DomainJoined: {IsDomainJoined}.",
            snapshot.MachineName,
            snapshot.IsDomainJoined);

        return Task.FromResult(snapshot);
    }

    private static bool IsDomainJoined()
    {
        return !string.Equals(
            Environment.UserDomainName,
            Environment.MachineName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static (double totalGb, double freeGb) GetSystemDriveInfo()
    {
        var systemRoot = Path.GetPathRoot(Environment.SystemDirectory);
        if (string.IsNullOrWhiteSpace(systemRoot))
        {
            return (0, 0);
        }

        var drive = new DriveInfo(systemRoot);
        if (!drive.IsReady)
        {
            return (0, 0);
        }

        return
        (
            totalGb: Math.Round(drive.TotalSize / 1024d / 1024d / 1024d, 1),
            freeGb: Math.Round(drive.AvailableFreeSpace / 1024d / 1024d / 1024d, 1)
        );
    }
}
