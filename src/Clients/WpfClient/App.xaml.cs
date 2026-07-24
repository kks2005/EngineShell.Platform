using EngineShell.Application;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace WpfClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build DI container
        var services = new ServiceCollection();
        DIRegistration.RegisterServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Resolve ShellView from DI
        var shell = _serviceProvider.GetRequiredService<ShellView>();
        shell.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();

        base.OnExit(e);
    }
}

