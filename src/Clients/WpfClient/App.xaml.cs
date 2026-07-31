using AI.Adapter.Ollama;
using Engine.Adapter.PInvoke;
using Engine.Contracts;
using EngineShell.Application;
using EngineShell.Application.Exceptions;
using EngineShell.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using Presentation;
using Presentation.Dialogs;
using WpfClient.Services;
using System.Windows;
using System.Windows.Threading;

namespace WpfClient;

/// <summary>
/// Interaction logic for App.xaml.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private Logger? _fileLogger;
    private LoggingLevelSwitch? _levelSwitch;
    private ILogger<App>? _logger;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        var logPath = ConfigureLogging(services);

        services.AddApplication();
        services.AddSingleton<IDialogService, WpfDialogService>();
        services.AddPresentation(includeAIChat: true);
        services.AddSingleton<IProcessingEngine, PInvokeEngineAdapter>();
        services.AddSingleton(sp =>
        {
            var baseAddress =
                Environment.GetEnvironmentVariable("OLLAMA_URL")
                ?? "http://localhost:11434/";

            return new HttpClient
            {
                BaseAddress = new Uri(baseAddress)
            };
        });
        services.AddSingleton<IAIService>(sp =>
            new OllamaAIService(
                sp.GetRequiredService<HttpClient>(),
                Environment.GetEnvironmentVariable("OLLAMA_MODEL")
                    ?? "llama3.2"));
        services.AddSingleton<OllamaLocalHost>();
        services.AddSingleton<ShellView>();

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        RegisterExceptionHandlers();

        _logger.LogInformation(
            "WPF client started. AppVersion={AppVersion} "
            + "Runtime={Runtime} OS={OperatingSystem} "
            + "ProcessArchitecture={ProcessArchitecture} "
            + "Adapter={Adapter} LogPath={LogPath} LogLevel={LogLevel}",
            typeof(App).Assembly.GetName().Version?.ToString()
                ?? "unknown",
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture,
            nameof(PInvokeEngineAdapter),
            logPath,
            _levelSwitch!.MinimumLevel);

        var shell = _serviceProvider.GetRequiredService<ShellView>();
        shell.Show();

        await StartOllamaAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation(
            "WPF client exiting with code {ExitCode}.",
            e.ApplicationExitCode);

        UnregisterExceptionHandlers();
        _serviceProvider?.Dispose();
        _fileLogger?.Dispose();
        base.OnExit(e);
    }

    private async Task StartOllamaAsync()
    {
        try
        {
            var host = _serviceProvider!
                .GetRequiredService<OllamaLocalHost>();

            if (!await host.EnsureRunningAsync())
            {
                _logger?.LogWarning(
                    "Ollama was started, but its local API did not "
                    + "become available.");

                MessageBox.Show(
                    "Ollama was started, but its local API did not become "
                    + "available.",
                    "Ollama unavailable",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Win32Exception exception)
        {
            _logger?.LogWarning(
                exception,
                "Ollama is not installed or is unavailable on PATH.");

            MessageBox.Show(
                "Ollama is not installed or is not available on PATH. "
                + "Install Ollama and restart this application.",
                "Ollama not installed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            _logger?.LogError(
                exception,
                "Unexpected error while starting Ollama.");

            MessageBox.Show(
                "Ollama could not be started. See the application log "
                + "for details.",
                "Ollama unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private string ConfigureLogging(IServiceCollection services)
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "EngineShell.Platform",
            "Logs");
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, "WpfClient-.log");

        _levelSwitch = new LoggingLevelSwitch(
            Enum.TryParse<LogEventLevel>(
                Environment.GetEnvironmentVariable("ENGINESHELL_LOG_LEVEL"),
                ignoreCase: true,
                out var level)
            ? level
            : LogEventLevel.Debug);

        _fileLogger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_levelSwitch)
            .Enrich.FromLogContext()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} "
                    + "[{Level:u3}] {SourceContext} {Message:lj} "
                    + "{Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(_fileLogger, dispose: false);
        });

        return logPath;
    }

    private void RegisterExceptionHandlers()
    {
        DispatcherUnhandledException +=
            OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException +=
            OnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException +=
            OnUnhandledDomainException;
    }

    private void UnregisterExceptionHandlers()
    {
        DispatcherUnhandledException -=
            OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException -=
            OnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException -=
            OnUnhandledDomainException;
    }

    private void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        var operationId = CreateOperationId();

        using (_logger?.BeginScope(
            new Dictionary<string, object?>
            {
                ["OperationId"] = operationId
            }))
        {
            _logger?.LogCritical(
                e.Exception,
                "Unhandled WPF dispatcher exception.");
        }

        e.Handled = true;

        MessageBox.Show(
            "The application encountered an unexpected error and will "
            + $"close. Reference: {operationId}",
            "Unexpected error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        Shutdown(-1);
    }

    private void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(
            e.Exception,
            "Unobserved task exception.");
        e.SetObserved();
    }

    private void OnUnhandledDomainException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        _logger?.LogCritical(
            ex,
            "Unhandled AppDomain exception{Detail}. IsTerminating={IsTerminating}",
            ex is null ? $": {e.ExceptionObject}" : string.Empty,
            e.IsTerminating);
    }

    private static string CreateOperationId() => OperationId.Create();
}
