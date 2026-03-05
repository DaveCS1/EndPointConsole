using EndpointConsole.Core.Models;

namespace EndpointConsole.Core.Interfaces;

public interface IMachineSnapshotProvider
{
    Task<MachineSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
