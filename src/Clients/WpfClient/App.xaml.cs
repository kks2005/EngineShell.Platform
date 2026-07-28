using Engine.Adapter.PInvoke;
using Engine.Contracts;
using EngineShell.Application;
using Microsoft.Extensions.DependencyInjection;
using Presentation;
using System.Windows;

namespace WpfClient;

/// <summary>
/// Interaction logic for App.xaml.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AddApplication();
        services.AddPresentation();
        services.AddSingleton<IProcessingEngine, PInvokeEngineAdapter>();
        services.AddSingleton<ShellView>();

        _serviceProvider = services.BuildServiceProvider();

        var shell = _serviceProvider.GetRequiredService<ShellView>();
        shell.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
