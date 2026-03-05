using System.IO;
using System.Windows;
using System.Windows.Threading;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Wpf.Services;
using EndpointConsole.Wpf.ViewModels;
using EndpointConsole.WindowsSystem.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EndpointConsole.Wpf;

public partial class App : Application
{
    private IHost? _host;
    private ILogger<App>? _logger;
    private string _logDirectory = string.Empty;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EndpointConsole",
            "Logs");
        Directory.CreateDirectory(_logDirectory);

        var logFilePath = Path.Combine(_logDirectory, "endpointconsole-.log");

        _host = Host.CreateDefaultBuilder()
            .UseSerilog((_, _, loggerConfiguration) =>
            {
                loggerConfiguration
                    .MinimumLevel.Debug()
                    .Enrich.FromLogContext()
                    .WriteTo.Debug()
                    .WriteTo.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        shared: true,
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}");
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IOperationExecutor, OperationExecutor>();
                services.AddSingleton<IMachineSnapshotProvider, MachineSnapshotProvider>();
                services.AddSingleton<IServiceManager, ServiceManager>();
                services.AddSingleton<IEventLogReader, EventLogReader>();
                services.AddSingleton<IRegistryConfigStore, RegistryConfigStore>();
                services.AddSingleton<IFolderAclInspector, FolderAclInspector>();
                services.AddSingleton<ISessionEnumerator, SessionEnumerator>();
                services.AddSingleton<IDiagnosticsBundleBuilder, DiagnosticsBundleBuilder>();

                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<ServicesViewModel>();
                services.AddSingleton<DiagnosticsViewModel>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        _logger = _host.Services.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("Application started. Logs path: {LogDirectory}", _logDirectory);

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnTaskSchedulerUnobservedTaskException;

        if (_host is not null)
        {
            _logger?.LogInformation("Application shutdown initiated.");
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogCritical(e.Exception, "Unhandled UI exception.");
        ShowUnexpectedErrorMessage();
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
                        ?? new InvalidOperationException("Unhandled non-exception error object received.");

        _logger?.LogCritical(exception, "Unhandled AppDomain exception. Terminating: {IsTerminating}", e.IsTerminating);
        ShowUnexpectedErrorMessage();
    }

    private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unobserved task exception.");
        e.SetObserved();
    }

    private void ShowUnexpectedErrorMessage()
    {
        MessageBox.Show(
            $"An unexpected error occurred. Review logs in:{Environment.NewLine}{_logDirectory}",
            "EndpointConsole Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
