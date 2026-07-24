using EngineShell.Application.Interfaces;
using EngineShell.Application.Models;
using EngineShell.Application.Services;
using Microsoft.Extensions.DependencyInjection;


namespace EngineShell.Application;

public static class ServiceRegistration
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IAppStatusService, AppStatusService>();
        services.AddSingleton<INavigationService, NavigationService>();
    }
}