using System.Security.AccessControl;
using System.Security.Principal;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using Microsoft.Extensions.Logging;

namespace EndpointConsole.WindowsSystem.Services;

public sealed class FolderAclInspector(ILogger<FolderAclInspector> logger) : IFolderAclInspector
{
    private readonly ILogger<FolderAclInspector> _logger = logger;

    public Task<IReadOnlyList<FolderAclIssue>> InspectAsync(
        IEnumerable<string> paths,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<FolderAclIssue>();

        foreach (var path in paths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            InspectPath(path, issues);
        }

        _logger.LogInformation("ACL inspection completed. Issues found: {Count}.", issues.Count);
        return Task.FromResult<IReadOnlyList<FolderAclIssue>>(issues);
    }

    private void InspectPath(string path, ICollection<FolderAclIssue> issues)
    {
        if (!Directory.Exists(path))
        {
            issues.Add(new FolderAclIssue
            {
                Path = path,
                Identity = "N/A",
                Rights = "N/A",
                Issue = "Directory not found."
            });
            return;
        }

        try
        {
            var security = new DirectoryInfo(path).GetAccessControl();
            var rules = security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(NTAccount))
                .Cast<FileSystemAccessRule>()
                .ToList();

            var everyoneFullControl = rules.Any(rule =>
                rule.AccessControlType == AccessControlType.Allow &&
                rule.IdentityReference.Value.EndsWith("Everyone", StringComparison.OrdinalIgnoreCase) &&
                rule.FileSystemRights.HasFlag(FileSystemRights.FullControl));

            if (everyoneFullControl)
            {
                issues.Add(new FolderAclIssue
                {
                    Path = path,
                    Identity = "Everyone",
                    Rights = FileSystemRights.FullControl.ToString(),
                    Issue = "Overly broad permission detected."
                });
            }

            var usersCanRead = rules.Any(rule =>
                rule.AccessControlType == AccessControlType.Allow &&
                rule.IdentityReference.Value.EndsWith("Users", StringComparison.OrdinalIgnoreCase) &&
                (rule.FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute) ||
                 rule.FileSystemRights.HasFlag(FileSystemRights.Read)));

            if (!usersCanRead)
            {
                issues.Add(new FolderAclIssue
                {
                    Path = path,
                    Identity = "Users",
                    Rights = FileSystemRights.ReadAndExecute.ToString(),
                    Issue = "Expected read permission for Users group was not found."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ACL inspection failed for {Path}.", path);
            issues.Add(new FolderAclIssue
            {
                Path = path,
                Identity = "N/A",
                Rights = "N/A",
                Issue = ex.Message
            });
        }
    }
}
