using EndpointConsole.Core.Models;

namespace EndpointConsole.Core.Interfaces;

public interface IDiagnosticsBundleBuilder
{
    Task<DiagnosticsBundleManifest> BuildAsync(
        string outputDirectory,
        CancellationToken cancellationToken = default);

    Task<DiagnosticsBundleManifest> BuildAsync(
        string outputDirectory,
        DiagnosticsBuildMode buildMode,
        CancellationToken cancellationToken = default);
}
