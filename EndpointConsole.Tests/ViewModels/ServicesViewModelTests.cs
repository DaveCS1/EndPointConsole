using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using EndpointConsole.Tests.Helpers;
using EndpointConsole.Wpf.ViewModels;
using Moq;

namespace EndpointConsole.Tests.ViewModels;

public class ServicesViewModelTests
{
    [Test]
    public async Task RefreshCommand_LoadsServicesFromManager()
    {
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
                    StartType = "Automatic",
                    CanStop = true
                },
                new ServiceInfo
                {
                    ServiceName = "WinRM",
                    DisplayName = "Windows Remote Management",
                    Status = "Stopped",
                    StartType = "Manual",
                    CanStop = false
                }
            ]);
        serviceManager.Setup(manager => manager.StartServiceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        serviceManager.Setup(manager => manager.StopServiceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        serviceManager.Setup(manager => manager.RestartServiceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = new ServicesViewModel(new InlineOperationExecutor(), serviceManager.Object);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.That(viewModel.Services.Count, Is.EqualTo(2));
        Assert.That(viewModel.Services[0].ServiceName, Is.EqualTo("Spooler"));
    }

    [Test]
    public async Task StartSelectedServiceCommand_WithNoSelection_DoesNotInvokeManager()
    {
        var serviceManager = new Mock<IServiceManager>();
        serviceManager
            .Setup(manager => manager.ListServicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ServiceInfo>());

        var viewModel = new ServicesViewModel(new InlineOperationExecutor(), serviceManager.Object);
        await viewModel.StartSelectedServiceCommand.ExecuteAsync(null);

        serviceManager.Verify(
            manager => manager.StartServiceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
