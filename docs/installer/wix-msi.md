# EndpointConsole MSI Packaging

## Build installer

```powershell
dotnet build EndpointConsole.Installer\EndpointConsole.Installer.wixproj -c Release
```

Output MSI:

`EndpointConsole.Installer\bin\x64\Release\EndpointConsole.Installer.msi`

## Build with explicit product version

```powershell
dotnet build EndpointConsole.Installer\EndpointConsole.Installer.wixproj -c Release -p:ProductVersion=1.0.0
dotnet build EndpointConsole.Installer\EndpointConsole.Installer.wixproj -c Release -p:ProductVersion=1.1.0
```

`MajorUpgrade` is enabled, so `1.1.0` upgrades `1.0.0` and downgrades are blocked.

## Installed artifacts

- Application files under `Program Files\EndpointConsole`
- Start Menu shortcut: `EndpointConsole`
- Registry values:
  - `HKLM\Software\EndpointConsole\InstallPath`
  - `HKLM\Software\EndpointConsole\Version`
