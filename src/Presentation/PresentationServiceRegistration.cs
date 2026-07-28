using EngineShell.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using Presentation.ViewModels;

namespace Presentation;

public static class PresentationServiceRegistration
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services)
    {
        services.AddSingleton<HeaderViewModel>();
        services.AddSingleton<FooterViewModel>();
        services.AddSingleton<GeneralViewModel>();
        services.AddSingleton<ScreensViewModel>();
        services.AddSingleton<RenderEngineViewModel>();

        services.AddSingleton<NavigationItem>(sp =>
            new NavigationItem(
                "General",
                "\uE713",
                sp.GetRequiredService<GeneralViewModel>()));

        services.AddSingleton<NavigationItem>(sp =>
            new NavigationItem(
                "Screens",
                "\uE771",
                sp.GetRequiredService<ScreensViewModel>()));

        services.AddSingleton<NavigationItem>(sp =>
            new NavigationItem(
                "RenderEngine",
                "\uE8B5",
                sp.GetRequiredService<RenderEngineViewModel>()));

        services.AddSingleton<ShellViewModel>();

        return services;
    }
}
