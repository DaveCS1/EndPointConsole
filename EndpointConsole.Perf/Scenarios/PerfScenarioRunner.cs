using System.Diagnostics;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using EndpointConsole.WindowsSystem.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EndpointConsole.Perf.Scenarios;

public static class PerfScenarioRunner
{
    public static async Task RunAsync(string[] args)
    {
        var options = PerfOptions.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var logFilePath = Path.Combine(
            options.OutputDirectory,
            $"perf-results-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");

        using var host = BuildHost();
        await host.StartAsync();

        var bundleBuilder = host.Services.GetRequiredService<IDiagnosticsBundleBuilder>();

        await WriteAsync(logFilePath, $"Start: {DateTimeOffset.Now:O}");
        await WriteAsync(
            logFilePath,
            $"Iterations={options.Iterations}, Warmup={options.WarmupIterations}, Mode={options.Mode}, Output={options.OutputDirectory}");

        var results = new List<ScenarioResult>();
        if (options.Mode is PerfExecutionMode.Baseline or PerfExecutionMode.Both)
        {
            results.Add(await Scenario_CollectBundle_Baseline(
                bundleBuilder,
                options.Iterations,
                options.WarmupIterations,
                options.OutputDirectory,
                logFilePath,
                CancellationToken.None));
        }

        if (options.Mode is PerfExecutionMode.Optimized or PerfExecutionMode.Both)
        {
            results.Add(await Scenario_CollectBundle_Optimized(
                bundleBuilder,
                options.Iterations,
                options.WarmupIterations,
                options.OutputDirectory,
                logFilePath,
                CancellationToken.None));
        }

        foreach (var result in results)
        {
            await WriteAsync(logFilePath, result.ToSummaryLine());
        }

        Console.WriteLine();
        foreach (var result in results)
        {
            Console.WriteLine(result.ToSummaryLine());
        }
        Console.WriteLine($"Log file: {logFilePath}");

        await host.StopAsync();
    }

    private static async Task<ScenarioResult> Scenario_CollectBundle_Baseline(
        IDiagnosticsBundleBuilder builder,
        int iterations,
        int warmupIterations,
        string outputDirectory,
        string logFilePath,
        CancellationToken cancellationToken)
    {
        return await RunCollectBundleScenarioAsync(
            builder,
            DiagnosticsBuildMode.Baseline,
            "Scenario_CollectBundle_Baseline",
            iterations,
            warmupIterations,
            outputDirectory,
            logFilePath,
            cancellationToken);
    }

    private static async Task<ScenarioResult> Scenario_CollectBundle_Optimized(
        IDiagnosticsBundleBuilder builder,
        int iterations,
        int warmupIterations,
        string outputDirectory,
        string logFilePath,
        CancellationToken cancellationToken)
    {
        return await RunCollectBundleScenarioAsync(
            builder,
            DiagnosticsBuildMode.Optimized,
            "Scenario_CollectBundle_Optimized",
            iterations,
            warmupIterations,
            outputDirectory,
            logFilePath,
            cancellationToken);
    }

    private static async Task<ScenarioResult> RunCollectBundleScenarioAsync(
        IDiagnosticsBundleBuilder builder,
        DiagnosticsBuildMode mode,
        string scenarioName,
        int iterations,
        int warmupIterations,
        string outputDirectory,
        string logFilePath,
        CancellationToken cancellationToken)
    {
        var scenarioOutputDir = Path.Combine(outputDirectory, mode.ToString().ToLowerInvariant());
        Directory.CreateDirectory(scenarioOutputDir);

        await WriteAsync(logFilePath, $"Running {scenarioName}...");
        Console.WriteLine($"Running {scenarioName}...");

        for (var warmup = 1; warmup <= warmupIterations; warmup++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await builder.BuildAsync(scenarioOutputDir, mode, cancellationToken);
            await WriteAsync(logFilePath, $"  Warmup {warmup}/{warmupIterations} completed.");
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var timings = new List<double>(iterations);
        for (var iteration = 1; iteration <= iterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();
            var manifest = await builder.BuildAsync(scenarioOutputDir, mode, cancellationToken);
            stopwatch.Stop();

            if (!File.Exists(manifest.OutputZipPath))
            {
                throw new InvalidOperationException($"Expected bundle output not found: {manifest.OutputZipPath}");
            }

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            timings.Add(elapsedMs);
            var line = $"  Iteration {iteration}/{iterations}: {elapsedMs:N2} ms";
            Console.WriteLine(line);
            await WriteAsync(logFilePath, line);
        }

        return ScenarioResult.FromTimings(scenarioName, mode, timings);
    }

    private static IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IMachineSnapshotProvider, MachineSnapshotProvider>();
                services.AddSingleton<IServiceManager, ServiceManager>();
                services.AddSingleton<IEventLogReader, EventLogReader>();
                services.AddSingleton<IRegistryConfigStore, RegistryConfigStore>();
                services.AddSingleton<IFolderAclInspector, FolderAclInspector>();
                services.AddSingleton<ISessionEnumerator, SessionEnumerator>();
                services.AddSingleton<IDiagnosticsBundleBuilder, DiagnosticsBundleBuilder>();
            })
            .Build();
    }

    private static Task WriteAsync(string logFilePath, string message)
    {
        var line = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}";
        return File.AppendAllTextAsync(logFilePath, line);
    }

    private sealed record PerfOptions(int Iterations, int WarmupIterations, string OutputDirectory, PerfExecutionMode Mode)
    {
        public static PerfOptions Parse(string[] args)
        {
            var iterations = 8;
            var warmup = 1;
            var mode = PerfExecutionMode.Both;
            var output = Path.Combine(AppContext.BaseDirectory, "perf_logs");

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                if (arg.Equals("--iterations", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
                {
                    _ = int.TryParse(args[++index], out iterations);
                    continue;
                }

                if (arg.Equals("--warmup", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
                {
                    _ = int.TryParse(args[++index], out warmup);
                    continue;
                }

                if (arg.Equals("--output", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
                {
                    output = args[++index];
                    continue;
                }

                if (arg.Equals("--mode", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
                {
                    mode = ParseMode(args[++index]);
                }
            }

            iterations = Math.Max(iterations, 1);
            warmup = Math.Max(warmup, 0);
            return new PerfOptions(iterations, warmup, output, mode);
        }

        private static PerfExecutionMode ParseMode(string mode)
        {
            return mode.ToLowerInvariant() switch
            {
                "baseline" => PerfExecutionMode.Baseline,
                "optimized" => PerfExecutionMode.Optimized,
                _ => PerfExecutionMode.Both
            };
        }
    }

    private enum PerfExecutionMode
    {
        Baseline,
        Optimized,
        Both
    }

    private sealed record ScenarioResult(
        string ScenarioName,
        DiagnosticsBuildMode Mode,
        double MinMs,
        double MaxMs,
        double MeanMs,
        double MedianMs,
        double TotalMs,
        int Iterations)
    {
        public static ScenarioResult FromTimings(
            string scenarioName,
            DiagnosticsBuildMode mode,
            IReadOnlyList<double> timings)
        {
            var ordered = timings.OrderBy(value => value).ToArray();
            var min = ordered.First();
            var max = ordered.Last();
            var total = ordered.Sum();
            var mean = total / ordered.Length;
            var median = ordered.Length % 2 == 0
                ? (ordered[(ordered.Length / 2) - 1] + ordered[ordered.Length / 2]) / 2.0
                : ordered[ordered.Length / 2];

            return new ScenarioResult(
                scenarioName,
                mode,
                min,
                max,
                mean,
                median,
                total,
                ordered.Length);
        }

        public string ToSummaryLine()
        {
            return
                $"{ScenarioName} ({Mode}): Iter={Iterations}, Total={TotalMs:N2} ms, Mean={MeanMs:N2} ms, Median={MedianMs:N2} ms, Min={MinMs:N2} ms, Max={MaxMs:N2} ms";
        }
    }
}
