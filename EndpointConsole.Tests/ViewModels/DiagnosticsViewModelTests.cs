using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using EndpointConsole.Tests.Helpers;
using EndpointConsole.Wpf.Services;
using EndpointConsole.Wpf.ViewModels;
using Moq;

namespace EndpointConsole.Tests.ViewModels;

public class DiagnosticsViewModelTests
{
    [Test]
    public async Task RefreshDiagnosticsCommand_PopulatesCollectionsAndSetsWarningForErrorEvents()
    {
        var eventLogReader = new Mock<IEventLogReader>();
        eventLogReader
            .Setup(reader => reader.GetRecentEntriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new EventLogRecord
                {
                    EventId = 1001,
                    Level = "Error",
                    Message = "Sample error",
                    Source = "UnitTest"
                }
            ]);

        var registryStore = new Mock<IRegistryConfigStore>();
        registryStore
            .Setup(store => store.ReadValuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?> { ["Sample"] = "Value" });
        registryStore
            .Setup(store => store.WriteValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var bundleBuilder = CreateBundleBuilderMock().Object;
        var viewModel = new DiagnosticsViewModel(
            new InlineOperationExecutor(),
            eventLogReader.Object,
            registryStore.Object,
            bundleBuilder);

        await viewModel.RefreshDiagnosticsCommand.ExecuteAsync(null);

        Assert.That(viewModel.RecentEvents.Count, Is.EqualTo(1));
        Assert.That(viewModel.RegistryValues.Count, Is.EqualTo(1));
        Assert.That(viewModel.OperationSeverity, Is.EqualTo("Warning"));
        Assert.That(viewModel.SeverityMessage, Is.Not.Empty);
    }

    [Test]
    public async Task SaveRegistryValueCommand_WhenValueNameMissing_SetsWarningAndSkipsWrite()
    {
        var registryStore = new Mock<IRegistryConfigStore>();
        registryStore
            .Setup(store => store.ReadValuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?>());

        var viewModel = new DiagnosticsViewModel(
            new InlineOperationExecutor(),
            CreateEventReaderMock().Object,
            registryStore.Object,
            CreateBundleBuilderMock().Object)
        {
            RegistryValueName = string.Empty
        };

        await viewModel.SaveRegistryValueCommand.ExecuteAsync(null);

        registryStore.Verify(
            store => store.WriteValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.That(viewModel.OperationSeverity, Is.EqualTo("Warning"));
        Assert.That(viewModel.SeverityMessage, Does.Contain("required"));
    }

    [Test]
    public async Task CollectDiagnosticsCommand_UsesSelectedBuildMode()
    {
        var capturedMode = DiagnosticsBuildMode.Optimized;
        var bundleBuilder = new Mock<IDiagnosticsBundleBuilder>();
        bundleBuilder
            .Setup(builder => builder.BuildAsync(It.IsAny<string>(), It.IsAny<DiagnosticsBuildMode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string output, DiagnosticsBuildMode mode, CancellationToken _) =>
            {
                capturedMode = mode;
                return new DiagnosticsBundleManifest
                {
                    MachineName = "test-machine",
                    OutputZipPath = Path.Combine(output, "bundle.zip"),
                    IncludedFiles = ["machine-snapshot.json"]
                };
            });
        bundleBuilder
            .Setup(builder => builder.BuildAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DiagnosticsBundleManifest());

        var viewModel = new DiagnosticsViewModel(
            new InlineOperationExecutor(),
            CreateEventReaderMock().Object,
            CreateRegistryStoreMock().Object,
            bundleBuilder.Object)
        {
            SelectedBuildMode = DiagnosticsBuildMode.Baseline
        };

        await viewModel.CollectDiagnosticsCommand.ExecuteAsync(null);

        Assert.That(capturedMode, Is.EqualTo(DiagnosticsBuildMode.Baseline));
        Assert.That(viewModel.OperationStatus, Does.Contain("Baseline bundle created"));
    }

    [Test]
    public async Task CollectDiagnosticsCommand_CanBeCancelled()
    {
        var bundleBuilder = new Mock<IDiagnosticsBundleBuilder>();
        bundleBuilder
            .Setup(builder => builder.BuildAsync(It.IsAny<string>(), It.IsAny<DiagnosticsBuildMode>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, DiagnosticsBuildMode _, CancellationToken cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                return new DiagnosticsBundleManifest();
            });
        bundleBuilder
            .Setup(builder => builder.BuildAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DiagnosticsBundleManifest());

        var viewModel = new DiagnosticsViewModel(
            new InlineOperationExecutor(),
            CreateEventReaderMock().Object,
            CreateRegistryStoreMock().Object,
            bundleBuilder.Object);

        var collectTask = viewModel.CollectDiagnosticsCommand.ExecuteAsync(null);
        await TestAwaiter.WaitUntilAsync(() => viewModel.IsCollecting, TimeSpan.FromSeconds(3));

        Assert.That(viewModel.CollectDiagnosticsCommand.CanExecute(null), Is.False);
        viewModel.CancelCollectionCommand.Execute(null);

        await collectTask;

        Assert.That(viewModel.IsCollecting, Is.False);
        Assert.That(viewModel.OperationStatus, Is.EqualTo("Collection cancelled."));
        Assert.That(viewModel.OperationSeverity, Is.EqualTo("Warning"));
    }

    private static Mock<IEventLogReader> CreateEventReaderMock()
    {
        var mock = new Mock<IEventLogReader>();
        mock.Setup(reader => reader.GetRecentEntriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EventLogRecord>());
        return mock;
    }

    private static Mock<IRegistryConfigStore> CreateRegistryStoreMock()
    {
        var mock = new Mock<IRegistryConfigStore>();
        mock.Setup(store => store.ReadValuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?>());
        mock.Setup(store => store.WriteValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static Mock<IDiagnosticsBundleBuilder> CreateBundleBuilderMock()
    {
        var mock = new Mock<IDiagnosticsBundleBuilder>();
        mock.Setup(builder => builder.BuildAsync(It.IsAny<string>(), It.IsAny<DiagnosticsBuildMode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string output, DiagnosticsBuildMode _, CancellationToken _) => new DiagnosticsBundleManifest
            {
                MachineName = "test-machine",
                OutputZipPath = Path.Combine(output, "bundle.zip"),
                IncludedFiles = ["machine-snapshot.json"]
            });
        mock.Setup(builder => builder.BuildAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DiagnosticsBundleManifest());
        return mock;
    }
}
