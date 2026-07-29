using AI.Adapter.Ollama;
using Engine.Adapter.PInvoke;
using Engine.Contracts;
using EngineShell.Application;
using EngineShell.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Net.Http;
using Presentation;
using System.Windows;

namespace WpfClient;

/// <summary>
/// Interaction logic for App.xaml.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AddApplication();
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

        var shell = _serviceProvider.GetRequiredService<ShellView>();
        shell.Show();

        await StartOllamaAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
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
                MessageBox.Show(
                    "Ollama was started, but its local API did not become "
                    + "available.",
                    "Ollama unavailable",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Win32Exception)
        {
            MessageBox.Show(
                "Ollama is not installed or is not available on PATH. "
                + "Install Ollama and restart this application.",
                "Ollama not installed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Ollama unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
