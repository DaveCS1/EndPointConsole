using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using EndpointConsole.Wpf.Services;

namespace EndpointConsole.Wpf.ViewModels;

public partial class DiagnosticsViewModel : ViewModelBase
{
    private readonly IOperationExecutor _operationExecutor;
    private readonly IEventLogReader _eventLogReader;
    private readonly IRegistryConfigStore _registryConfigStore;
    private readonly IDiagnosticsBundleBuilder _diagnosticsBundleBuilder;
    private CancellationTokenSource? _collectionCancellationTokenSource;

    public DiagnosticsViewModel(
        IOperationExecutor operationExecutor,
        IEventLogReader eventLogReader,
        IRegistryConfigStore registryConfigStore,
        IDiagnosticsBundleBuilder diagnosticsBundleBuilder)
    {
        _operationExecutor = operationExecutor;
        _eventLogReader = eventLogReader;
        _registryConfigStore = registryConfigStore;
        _diagnosticsBundleBuilder = diagnosticsBundleBuilder;

        RecentEvents = new ObservableCollection<EventLogRecord>();
        RegistryValues = new ObservableCollection<RegistryItemViewModel>();
        _ = RefreshDiagnosticsAsync();
    }

    [ObservableProperty]
    private string operationStatus = "Load diagnostics to begin.";

    [ObservableProperty]
    private string operationSeverity = "Info";

    [ObservableProperty]
    private string severityMessage = string.Empty;

    [ObservableProperty]
    private bool isCollecting;

    [ObservableProperty]
    private double collectionProgress;

    [ObservableProperty]
    private bool isCollectionProgressIndeterminate;

    [ObservableProperty]
    private string latestBundlePath = "No bundle generated yet.";

    [ObservableProperty]
    private DiagnosticsBuildMode selectedBuildMode = DiagnosticsBuildMode.Optimized;

    [ObservableProperty]
    private string registryKeyPath = @"HKCU\SOFTWARE\EndpointConsole";

    [ObservableProperty]
    private string registryValueName = "SampleMode";

    [ObservableProperty]
    private string registryValueData = "Enabled";

    public ObservableCollection<EventLogRecord> RecentEvents { get; }

    public ObservableCollection<RegistryItemViewModel> RegistryValues { get; }

    public IReadOnlyList<DiagnosticsBuildMode> BuildModes { get; } =
        [DiagnosticsBuildMode.Baseline, DiagnosticsBuildMode.Optimized];

    [RelayCommand]
    private async Task RefreshDiagnosticsAsync()
    {
        IReadOnlyList<EventLogRecord> events = Array.Empty<EventLogRecord>();
        IReadOnlyDictionary<string, string?> registryValues = new Dictionary<string, string?>();

        var result = await _operationExecutor.ExecuteAsync(
            operationName: "Diagnostics.Refresh",
            operation: async cancellationToken =>
            {
                events = await _eventLogReader.GetRecentEntriesAsync(50, cancellationToken);
                registryValues = await _registryConfigStore.ReadValuesAsync(RegistryKeyPath, cancellationToken);
            });

        var correlationFragment = result.CorrelationId.ToString("N")[..8];
        if (!result.Succeeded)
        {
            OperationSeverity = "Error";
            SeverityMessage = $"Diagnostics refresh failed. ID:{correlationFragment}";
            OperationStatus = result.ErrorMessage ?? "Diagnostics refresh failed.";
            return;
        }

        RecentEvents.Clear();
        foreach (var eventRecord in events)
        {
            RecentEvents.Add(eventRecord);
        }

        RegistryValues.Clear();
        foreach (var registryValue in registryValues)
        {
            RegistryValues.Add(new RegistryItemViewModel(registryValue.Key, registryValue.Value ?? string.Empty));
        }

        OperationSeverity = events.Any(eventRecord =>
            eventRecord.Level.Equals("Error", StringComparison.OrdinalIgnoreCase))
            ? "Warning"
            : "Info";

        SeverityMessage = OperationSeverity == "Warning"
            ? "Recent error events were detected."
            : string.Empty;

        OperationStatus =
            $"Loaded {RecentEvents.Count} events and {RegistryValues.Count} registry values  ID:{correlationFragment}";
    }

    [RelayCommand]
    private async Task SaveRegistryValueAsync()
    {
        if (string.IsNullOrWhiteSpace(RegistryValueName))
        {
            OperationSeverity = "Warning";
            SeverityMessage = "Registry value name is required.";
            return;
        }

        var result = await _operationExecutor.ExecuteAsync(
            operationName: "Diagnostics.Registry.WriteValue",
            operation: cancellationToken => _registryConfigStore.WriteValueAsync(
                RegistryKeyPath,
                RegistryValueName,
                RegistryValueData,
                cancellationToken));

        var correlationFragment = result.CorrelationId.ToString("N")[..8];
        if (!result.Succeeded)
        {
            OperationSeverity = "Error";
            SeverityMessage = $"Registry write failed. ID:{correlationFragment}";
            OperationStatus = result.ErrorMessage ?? "Registry write failed.";
            return;
        }

        OperationSeverity = "Info";
        SeverityMessage = string.Empty;
        OperationStatus = $"Registry value saved. ID:{correlationFragment}";
        await RefreshDiagnosticsAsync();
    }

    [RelayCommand(CanExecute = nameof(CanCollectDiagnostics))]
    private async Task CollectDiagnosticsAsync()
    {
        _collectionCancellationTokenSource?.Dispose();
        _collectionCancellationTokenSource = new CancellationTokenSource();

        IsCollecting = true;
        IsCollectionProgressIndeterminate = true;
        CollectionProgress = 10;
        OperationSeverity = "Info";
        SeverityMessage = string.Empty;
        OperationStatus = "Collecting diagnostics bundle...";

        DiagnosticsBundleManifest? manifest = null;
        var bundlesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EndpointConsole",
            "Bundles");

        var result = await _operationExecutor.ExecuteAsync(
            operationName: "Diagnostics.CollectBundle",
            operation: async cancellationToken =>
            {
                manifest = await _diagnosticsBundleBuilder.BuildAsync(
                    bundlesDirectory,
                    SelectedBuildMode,
                    cancellationToken);
            },
            cancellationToken: _collectionCancellationTokenSource.Token);

        var correlationFragment = result.CorrelationId.ToString("N")[..8];
        var cancelled = _collectionCancellationTokenSource.IsCancellationRequested;
        IsCollectionProgressIndeterminate = false;

        if (result.Succeeded && manifest is not null)
        {
            CollectionProgress = 100;
            LatestBundlePath = manifest.OutputZipPath;
            OperationStatus = $"{SelectedBuildMode} bundle created at {DateTimeOffset.Now:g}";
            OperationSeverity = "Info";
            SeverityMessage = $"Bundle ID:{correlationFragment}";
        }
        else if (cancelled)
        {
            CollectionProgress = 0;
            OperationStatus = "Collection cancelled.";
            OperationSeverity = "Warning";
            SeverityMessage = $"Cancelled ID:{correlationFragment}";
        }
        else
        {
            CollectionProgress = 0;
            OperationSeverity = "Error";
            SeverityMessage = $"Collection failed. ID:{correlationFragment}";
            OperationStatus = result.ErrorMessage ?? "Collection failed.";
        }

        IsCollecting = false;
        _collectionCancellationTokenSource.Dispose();
        _collectionCancellationTokenSource = null;
    }

    [RelayCommand(CanExecute = nameof(CanCancelCollection))]
    private void CancelCollection()
    {
        if (_collectionCancellationTokenSource is null)
        {
            return;
        }

        OperationStatus = "Cancellation requested...";
        _collectionCancellationTokenSource.Cancel();
    }

    private bool CanCollectDiagnostics()
    {
        return !IsCollecting;
    }

    private bool CanCancelCollection()
    {
        return IsCollecting &&
               _collectionCancellationTokenSource is not null &&
               !_collectionCancellationTokenSource.IsCancellationRequested;
    }

    partial void OnIsCollectingChanged(bool value)
    {
        CollectDiagnosticsCommand.NotifyCanExecuteChanged();
        CancelCollectionCommand.NotifyCanExecuteChanged();
    }

    public sealed record RegistryItemViewModel(string Name, string Value);
}
