using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using EndpointConsole.Wpf.Services;

namespace EndpointConsole.Wpf.ViewModels;

public partial class ServicesViewModel : ViewModelBase
{
    private readonly IOperationExecutor _operationExecutor;
    private readonly IServiceManager _serviceManager;

    [ObservableProperty]
    private string statusMessage = "Load services to begin.";

    [ObservableProperty]
    private ServiceRowViewModel? selectedService;

    public ServicesViewModel(
        IOperationExecutor operationExecutor,
        IServiceManager serviceManager)
    {
        _operationExecutor = operationExecutor;
        _serviceManager = serviceManager;
        Services = new ObservableCollection<ServiceRowViewModel>();
        _ = RefreshAsync();
    }

    public ObservableCollection<ServiceRowViewModel> Services { get; }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IReadOnlyList<ServiceInfo> serviceInfos = Array.Empty<ServiceInfo>();

        var result = await _operationExecutor.ExecuteAsync(
            operationName: "Services.Refresh",
            operation: async cancellationToken =>
            {
                serviceInfos = await _serviceManager.ListServicesAsync(cancellationToken);
            });

        var correlationFragment = result.CorrelationId.ToString("N")[..8];
        StatusMessage = result.Succeeded
            ? $"Loaded {serviceInfos.Count} services  ID:{correlationFragment}"
            : $"Refresh failed  ID:{correlationFragment}";

        if (!result.Succeeded)
        {
            return;
        }

        Services.Clear();
        foreach (var serviceInfo in serviceInfos)
        {
            Services.Add(new ServiceRowViewModel(serviceInfo));
        }
    }

    [RelayCommand]
    private async Task StartSelectedServiceAsync()
    {
        if (SelectedService is null)
        {
            return;
        }

        var serviceName = SelectedService.ServiceName;
        var result = await _operationExecutor.ExecuteAsync(
            operationName: $"Services.Start.{serviceName}",
            operation: async cancellationToken =>
            {
                await _serviceManager.StartServiceAsync(serviceName, cancellationToken);
            });

        var correlationFragment = result.CorrelationId.ToString("N")[..8];
        StatusMessage = result.Succeeded
            ? $"Started {serviceName}  ID:{correlationFragment}"
            : $"Start failed for {serviceName}  ID:{correlationFragment}";

        if (result.Succeeded)
        {
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task StopSelectedServiceAsync()
    {
        if (SelectedService is null)
        {
            return;
        }

        var serviceName = SelectedService.ServiceName;
        var result = await _operationExecutor.ExecuteAsync(
            operationName: $"Services.Stop.{serviceName}",
            operation: async cancellationToken =>
            {
                await _serviceManager.StopServiceAsync(serviceName, cancellationToken);
            });

        var correlationFragment = result.CorrelationId.ToString("N")[..8];
        StatusMessage = result.Succeeded
            ? $"Stopped {serviceName}  ID:{correlationFragment}"
            : $"Stop failed for {serviceName}  ID:{correlationFragment}";

        if (result.Succeeded)
        {
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task RestartSelectedServiceAsync()
    {
        if (SelectedService is null)
        {
            return;
        }

        var serviceName = SelectedService.ServiceName;
        var result = await _operationExecutor.ExecuteAsync(
            operationName: $"Services.Restart.{serviceName}",
            operation: async cancellationToken =>
            {
                await _serviceManager.RestartServiceAsync(serviceName, cancellationToken);
            });

        var correlationFragment = result.CorrelationId.ToString("N")[..8];
        StatusMessage = result.Succeeded
            ? $"Restarted {serviceName}  ID:{correlationFragment}"
            : $"Restart failed for {serviceName}  ID:{correlationFragment}";

        if (result.Succeeded)
        {
            await RefreshAsync();
        }
    }

    public sealed record ServiceRowViewModel
    {
        public ServiceRowViewModel(ServiceInfo serviceInfo)
        {
            ServiceName = serviceInfo.ServiceName;
            DisplayName = serviceInfo.DisplayName;
            Status = serviceInfo.Status;
            StartType = serviceInfo.StartType;
            CanStop = serviceInfo.CanStop;
        }

        public string ServiceName { get; }

        public string DisplayName { get; }

        public string Status { get; }

        public string StartType { get; }

        public bool CanStop { get; }
    }
}
