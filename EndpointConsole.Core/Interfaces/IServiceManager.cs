using EndpointConsole.Core.Models;

namespace EndpointConsole.Core.Interfaces;

public interface IServiceManager
{
    Task<IReadOnlyList<ServiceInfo>> ListServicesAsync(CancellationToken cancellationToken = default);

    Task StartServiceAsync(string serviceName, CancellationToken cancellationToken = default);

    Task StopServiceAsync(string serviceName, CancellationToken cancellationToken = default);

    Task RestartServiceAsync(string serviceName, CancellationToken cancellationToken = default);
}
