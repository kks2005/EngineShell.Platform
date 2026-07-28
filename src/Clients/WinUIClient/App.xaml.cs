using Engine.Adapter.PInvoke;
using Engine.Contracts;
using EngineShell.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Presentation;

namespace WinUIClient;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private Window? _mainWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        if (_mainWindow is not null)
        {
            _mainWindow.Activate();
            return;
        }

        var services = new ServiceCollection();

        services.AddApplication();
        services.AddPresentation();
        services.AddSingleton<IProcessingEngine, PInvokeEngineAdapter>();
        services.AddSingleton<ShellView>();

        _serviceProvider = services.BuildServiceProvider();

        _mainWindow = _serviceProvider.GetRequiredService<ShellView>();
        _mainWindow.Closed += OnMainWindowClosed;
        _mainWindow.Activate();
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        if (sender is Window window)
        {
            window.Closed -= OnMainWindowClosed;
        }

        _serviceProvider?.Dispose();
        _serviceProvider = null;
        _mainWindow = null;
    }
}
