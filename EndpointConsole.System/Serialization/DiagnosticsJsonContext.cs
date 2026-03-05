using System.Text.Json.Serialization;
using EndpointConsole.Core.Models;

namespace EndpointConsole.WindowsSystem.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MachineSnapshot))]
[JsonSerializable(typeof(List<ServiceInfo>))]
[JsonSerializable(typeof(List<EventLogRecord>))]
[JsonSerializable(typeof(DiagnosticsBundleManifest))]
[JsonSerializable(typeof(RegistrySnapshotData))]
internal partial class DiagnosticsJsonContext : JsonSerializerContext
{
}
