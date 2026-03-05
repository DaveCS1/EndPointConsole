# EndpointConsole Usage Guide

## 1. Prerequisites

Install the following tools:

- .NET 8 SDK
- Visual Studio 2022 (Desktop development with .NET)
- Git
- Optional for installer project in Visual Studio: WiX Toolset Visual Studio 2022 extension

Notes:

- The solution file is `EndPointConsole.sln`.
- If WiX extension is not installed, `EndpointConsole.Installer` may appear as incompatible in Visual Studio, but CLI builds still work.

## 2. Clone and Open

```powershell
git clone https://github.com/<your-user>/EndPointConsole.git
cd EndPointConsole
```

Open in Visual Studio:

```powershell
start EndPointConsole.sln
```

## 3. First-Time Local Build

```powershell
dotnet restore EndPointConsole.sln
dotnet build EndPointConsole.sln -c Debug
dotnet test EndpointConsole.Tests\EndpointConsole.Tests.csproj -c Debug
```

## 4. Run the WPF App

```powershell
dotnet run --project EndpointConsole.Wpf -c Debug
```

## 5. Git Setup for a New Local Repo

If this folder is not already a git repo:

```powershell
git init -b main
dotnet new gitignore
git add .
git commit -m "Initial commit"
```

Add GitHub remote and push:

```powershell
git remote add origin https://github.com/<your-user>/EndPointConsole.git
git push -u origin main
```

## 6. GitHub: Private or Public

Recommended flow:

1. Start as **private**.
2. Validate history and remove anything sensitive.
3. Make it public when ready:
   - GitHub -> Repo Settings -> General -> Danger Zone -> Change visibility.

## 7. Daily Dev Workflow

```powershell
git checkout main
git pull
git checkout -b feature/<short-name>
dotnet build EndPointConsole.sln -c Debug
dotnet test EndpointConsole.Tests\EndpointConsole.Tests.csproj -c Debug
git add .
git commit -m "Describe change"
git push -u origin feature/<short-name>
```

Then open a Pull Request.

## 8. Build Installer (MSI)

Batch helper:

```powershell
EndpointConsole.Installer\build.bat
EndpointConsole.Installer\build.bat Debug
EndpointConsole.Installer\build.bat Release 1.1.0
```

Direct command:

```powershell
dotnet build EndpointConsole.Installer\EndpointConsole.Installer.wixproj -c Release
```

MSI output:

`EndpointConsole.Installer\bin\x64\<Configuration>\EndpointConsole.Installer.msi`

See also:

- `docs/installer/wix-msi.md`

## 9. Run Performance Harness

Basic run:

```powershell
dotnet run --project EndpointConsole.Perf -c Release -- --iterations 10 --warmup 2 --mode both
```

Optional output override:

```powershell
dotnet run --project EndpointConsole.Perf -c Release -- --iterations 10 --warmup 2 --mode baseline --output C:\Perf\EndpointConsole
```

Default output when `--output` is omitted:

`EndpointConsole.Perf\bin\<Configuration>\net8.0-windows\perf_logs`

See also:

- `docs/perf/perfview.md`

## 10. Common Commands

Build release:

```powershell
dotnet build EndPointConsole.sln -c Release
```

Run all tests:

```powershell
dotnet test EndPointConsole.sln -c Debug
```

## 11. Troubleshooting

Installer project shows incompatible in Visual Studio:

- Install WiX VS extension, restart Visual Studio, reload solution.

Installer build fails in IDE but works in CLI:

- Build from terminal using `build.bat` or `dotnet build ...wixproj`.

WPF app does not launch:

- Confirm `net8.0-windows` targeting pack is installed and run `dotnet --info`.

Perf logs not found:

- Check `EndpointConsole.Perf\bin\<Configuration>\net8.0-windows\perf_logs` first.
