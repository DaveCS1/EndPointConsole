using EndpointConsole.Core.Models;

namespace EndpointConsole.Tests.Core;

public class MachineSnapshotSmokeTests
{
    [Test]
    public void MachineSnapshot_CanBeConstructed()
    {
        var snapshot = new MachineSnapshot
        {
            MachineName = "sample-machine",
            OsDescription = "Windows",
            OsVersion = "10.0",
            OsBuild = "22631",
            IsDomainJoined = false,
            Uptime = TimeSpan.FromHours(1)
        };

        Assert.That(snapshot.MachineName, Is.EqualTo("sample-machine"));
        Assert.That(snapshot.Uptime, Is.EqualTo(TimeSpan.FromHours(1)));
    }
}
