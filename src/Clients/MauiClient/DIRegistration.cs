using Engine.Adapter.PInvoke;
using Engine.Contracts;
using EngineShell.Application.Interfaces;
using EngineShell.Application.Models;
using EngineShell.Application.Services;
using Presentation.ViewModels;

namespace Clients.Maui;

public static class DIRegistration
{
    public static void RegisterServices(IServiceCollection services)
    {
        // engine services and adapters
        services.AddSingleton<IEngineEventBus, EngineEventBus>();
        services.AddSingleton<IProcessingEngine, PInvokeEngineAdapter>();
        services.AddSingleton<IProcessingService, ProcessingService>();

        // application services
        services.AddSingleton<IAppStatusService, AppStatusService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // Presentation (ViewModels)
        services.AddSingleton<HeaderViewModel>();
        services.AddSingleton<FooterViewModel>();

        services.AddSingleton<GeneralViewModel>();
        services.AddSingleton<ScreensViewModel>();
        services.AddSingleton<RenderEngineViewModel>();

        // Navigation Items (Presentation)
        services.AddSingleton<NavigationItem>(sp =>
            new NavigationItem("General", "\uE713", sp.GetRequiredService<GeneralViewModel>()));

        services.AddSingleton<NavigationItem>(sp =>
            new NavigationItem("Screens", "\uE771", sp.GetRequiredService<ScreensViewModel>()));

        services.AddSingleton<NavigationItem>(sp =>
            new NavigationItem("RenderEngine", "\uE8B5", sp.GetRequiredService<RenderEngineViewModel>()));

        // Shell (must be LAST)
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainPage>();
    }
}
