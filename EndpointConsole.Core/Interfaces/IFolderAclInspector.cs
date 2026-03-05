using EndpointConsole.Core.Models;

namespace EndpointConsole.Core.Interfaces;

public interface IFolderAclInspector
{
    Task<IReadOnlyList<FolderAclIssue>> InspectAsync(
        IEnumerable<string> paths,
        CancellationToken cancellationToken = default);
}
