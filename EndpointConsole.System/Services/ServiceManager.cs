using System.ServiceProcess;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using Microsoft.Extensions.Logging;

namespace EndpointConsole.WindowsSystem.Services;

public sealed class ServiceManager(ILogger<ServiceManager> logger) : IServiceManager
{
    private static readonly TimeSpan ServiceWaitTimeout = TimeSpan.FromSeconds(15);
    private readonly ILogger<ServiceManager> _logger = logger;

    public Task<IReadOnlyList<ServiceInfo>> ListServicesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var services = ServiceController.GetServices()
            .OrderBy(service => service.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(service => new ServiceInfo
            {
                ServiceName = service.ServiceName,
                DisplayName = service.DisplayName,
                Status = service.Status.ToString(),
                StartType = service.StartType.ToString(),
                CanStop = service.CanStop,
                CanPauseAndContinue = service.CanPauseAndContinue
            })
            .ToList();

        _logger.LogInformation("Retrieved {Count} services.", services.Count);
        return Task.FromResult<IReadOnlyList<ServiceInfo>>(services);
    }

    public Task StartServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var service = new ServiceController(serviceName);
        if (service.Status == ServiceControllerStatus.Running)
        {
            _logger.LogInformation("Service {ServiceName} is already running.", serviceName);
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting service {ServiceName}.", serviceName);
        service.Start();
        service.WaitForStatus(ServiceControllerStatus.Running, ServiceWaitTimeout);
        return Task.CompletedTask;
    }

    public Task StopServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var service = new ServiceController(serviceName);
        if (service.Status == ServiceControllerStatus.Stopped)
        {
            _logger.LogInformation("Service {ServiceName} is already stopped.", serviceName);
            return Task.CompletedTask;
        }

        _logger.LogInformation("Stopping service {ServiceName}.", serviceName);
        service.Stop();
        service.WaitForStatus(ServiceControllerStatus.Stopped, ServiceWaitTimeout);
        return Task.CompletedTask;
    }

    public async Task RestartServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Restarting service {ServiceName}.", serviceName);
        await StopServiceAsync(serviceName, cancellationToken);
        await StartServiceAsync(serviceName, cancellationToken);
    }
}
