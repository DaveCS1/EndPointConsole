# PerfView Workflow

## Prerequisites

1. Build release binaries:

```powershell
dotnet build EndPointConsole.sln -c Release
```

2. Accept PerfView EULA once:

```powershell
PerfView.exe /AcceptEula
```

## Repeatable Console Scenario (Preferred)

1. Run the harness directly to validate scenarios:

```powershell
dotnet run --project EndpointConsole.Perf -c Release -- --iterations 10 --warmup 2 --output C:\Perf\EndpointConsole
```

If `--output` is omitted, the harness writes to:

`EndpointConsole.Perf\bin\<Configuration>\net8.0-windows\perf_logs`

2. Collect baseline trace:

```powershell
PerfView.exe /NoGui /AcceptEula /BufferSizeMB:256 /MaxCollectSec:180 /DataFile:C:\Perf\traces\collectbundle-baseline-20260305.etl.zip collect "C:\Users\daves\source\repos\EndPointConsole\EndpointConsole.Perf\bin\Release\net8.0-windows\EndpointConsole.Perf.exe --iterations 10 --warmup 2 --mode baseline --output C:\Perf\EndpointConsole"
```

3. Collect optimized trace:

```powershell
PerfView.exe /NoGui /AcceptEula /BufferSizeMB:256 /MaxCollectSec:180 /DataFile:C:\Perf\traces\collectbundle-optimized-20260305.etl.zip collect "C:\Users\daves\source\repos\EndPointConsole\EndpointConsole.Perf\bin\Release\net8.0-windows\EndpointConsole.Perf.exe --iterations 10 --warmup 2 --mode optimized --output C:\Perf\EndpointConsole"
```

## WPF User-Path Scenario

1. Launch capture with enough time to click through UI:

```powershell
PerfView.exe /NoGui /AcceptEula /BufferSizeMB:256 /MaxCollectSec:240 /DataFile:C:\Perf\traces\wpf-collectdiagnostics-20260305.etl.zip collect "C:\Users\daves\source\repos\EndPointConsole\EndpointConsole.Wpf\bin\Release\net8.0-windows\EndpointConsole.Wpf.exe"
```

2. During capture:
   1. Open Diagnostics page.
   2. Select `Baseline`, click `Collect Diagnostics`.
   3. Repeat with `Optimized`.
   4. Wait for collection timeout to end the trace.

## Trace Naming Convention

Use this format:

1. `collectbundle-baseline-YYYYMMDD.etl.zip`
2. `collectbundle-optimized-YYYYMMDD.etl.zip`
3. `wpf-collectdiagnostics-YYYYMMDD.etl.zip`

## What To Inspect In PerfView

1. `CPU Stacks`: compare total CPU time between baseline and optimized traces.
2. `GC Heap Alloc Stacks`: compare allocation hot paths and total allocated bytes.
3. `Events -> GCStats`: compare GC count and pause impact.
4. `Thread Time Stacks`: verify zip/json paths are the dominant differences.

## Expected Output Artifacts

Perf harness output directory contains:

1. `baseline` bundles
2. `optimized` bundles
3. `perf-results-*.log` with per-iteration timings and summary stats
