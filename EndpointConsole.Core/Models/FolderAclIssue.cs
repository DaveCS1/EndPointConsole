namespace EndpointConsole.Core.Models;

public sealed record FolderAclIssue
{
    public string Path { get; init; } = string.Empty;

    public string Identity { get; init; } = string.Empty;

    public string Rights { get; init; } = string.Empty;

    public string Issue { get; init; } = string.Empty;
}
