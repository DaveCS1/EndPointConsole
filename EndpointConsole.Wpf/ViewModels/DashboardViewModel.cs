using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using EndpointConsole.Wpf.Services;

namespace EndpointConsole.Wpf.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IOperationExecutor _operationExecutor;
    private readonly IMachineSnapshotProvider _machineSnapshotProvider;
    private readonly IFolderAclInspector _folderAclInspector;
    private readonly ISessionEnumerator _sessionEnumerator;

    public DashboardViewModel(
        IOperationExecutor operationExecutor,
        IMachineSnapshotProvider machineSnapshotProvider,
        IFolderAclInspector folderAclInspector,
        ISessionEnumerator sessionEnumerator)
    {
        _operationExecutor = operationExecutor;
        _machineSnapshotProvider = machineSnapshotProvider;
        _folderAclInspector = folderAclInspector;
        _sessionEnumerator = sessionEnumerator;

        AclIssues = new ObservableCollection<FolderAclIssue>();
        Sessions = new ObservableCollection<UserSessionInfo>();
        _ = RefreshAsync();
    }

    [ObservableProperty]
    private string osSummary = "Not collected";

    [ObservableProperty]
    private string uptimeSummary = "Not collected";

    [ObservableProperty]
    private string activeSessionsSummary = "Not collected";

    [ObservableProperty]
    private string diskSummary = "Not collected";

    [ObservableProperty]
    private string lastUpdatedSummary = "Never";

    [ObservableProperty]
    private bool hasAclIssues;

    public ObservableCollection<FolderAclIssue> AclIssues { get; }

    public ObservableCollection<UserSessionInfo> Sessions { get; }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        MachineSnapshot? snapshot = null;
        IReadOnlyList<FolderAclIssue> aclIssues = Array.Empty<FolderAclIssue>();
        IReadOnlyList<UserSessionInfo> sessions = Array.Empty<UserSessionInfo>();

        var result = await _operationExecutor.ExecuteAsync(
            operationName: "Dashboard.Refresh",
            operation: async cancellationToken =>
            {
                snapshot = await _machineSnapshotProvider.GetSnapshotAsync(cancellationToken);
                sessions = await _sessionEnumerator.EnumerateSessionsAsync(cancellationToken);

                var pathsToInspect = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    AppContext.BaseDirectory
                };

                aclIssues = await _folderAclInspector.InspectAsync(pathsToInspect, cancellationToken);
            });

        var correlationFragment = result.CorrelationId.ToString("N")[..8];
        LastUpdatedSummary = result.Succeeded
            ? $"{DateTimeOffset.Now:g}  ID:{correlationFragment}"
            : $"Refresh failed  ID:{correlationFragment}";

        if (!result.Succeeded || snapshot is null)
        {
            return;
        }

        OsSummary = $"{snapshot.OsDescription} (Build {snapshot.OsBuild})";
        UptimeSummary = FormatUptime(snapshot.Uptime);
        var activeCount = sessions.Count(session => session.State.Equals("Active", StringComparison.OrdinalIgnoreCase));
        ActiveSessionsSummary = $"{activeCount} active / {sessions.Count} total";
        DiskSummary = $"{snapshot.SystemDriveFreeGb:0.0} GB free / {snapshot.SystemDriveTotalGb:0.0} GB";

        Sessions.Clear();
        foreach (var session in sessions)
        {
            Sessions.Add(session);
        }

        AclIssues.Clear();
        foreach (var issue in aclIssues)
        {
            AclIssues.Add(issue);
        }

        HasAclIssues = AclIssues.Count > 0;
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
    }
}
