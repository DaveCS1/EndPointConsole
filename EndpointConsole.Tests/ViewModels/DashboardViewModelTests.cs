using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using EndpointConsole.Tests.Helpers;
using EndpointConsole.Wpf.ViewModels;
using Moq;

namespace EndpointConsole.Tests.ViewModels;

public class DashboardViewModelTests
{
    [Test]
    public async Task RefreshCommand_PopulatesSnapshotSessionsAndAclIssues()
    {
        var snapshotProvider = new Mock<IMachineSnapshotProvider>();
        snapshotProvider
            .Setup(provider => provider.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MachineSnapshot
            {
                MachineName = "test-machine",
                OsDescription = "Windows Test",
                OsBuild = "26000",
                Uptime = TimeSpan.FromHours(30),
                SystemDriveFreeGb = 100,
                SystemDriveTotalGb = 512
            });

        var sessionEnumerator = new Mock<ISessionEnumerator>();
        sessionEnumerator
            .Setup(enumerator => enumerator.EnumerateSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new UserSessionInfo
                {
                    SessionId = 1,
                    UserName = "alice",
                    Domain = "CONTOSO",
                    State = "Active"
                },
                new UserSessionInfo
                {
                    SessionId = 2,
                    UserName = "bob",
                    Domain = "CONTOSO",
                    State = "Disconnected"
                }
            ]);

        var aclInspector = new Mock<IFolderAclInspector>();
        aclInspector
            .Setup(inspector => inspector.InspectAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new FolderAclIssue
                {
                    Path = @"C:\ProgramData",
                    Identity = "Everyone",
                    Rights = "FullControl",
                    Issue = "Overly broad permission detected."
                }
            ]);

        var viewModel = new DashboardViewModel(
            new InlineOperationExecutor(),
            snapshotProvider.Object,
            aclInspector.Object,
            sessionEnumerator.Object);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.That(viewModel.OsSummary, Does.Contain("Windows Test"));
        Assert.That(viewModel.ActiveSessionsSummary, Is.EqualTo("1 active / 2 total"));
        Assert.That(viewModel.DiskSummary, Does.Contain("100.0 GB free / 512.0 GB"));
        Assert.That(viewModel.Sessions.Count, Is.EqualTo(2));
        Assert.That(viewModel.AclIssues.Count, Is.EqualTo(1));
    }
}
