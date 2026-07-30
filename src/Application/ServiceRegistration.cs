using Engine.Contracts;
using EngineShell.Application.Interfaces;
using EngineShell.Application.Models;
using EngineShell.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EngineShell.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton<IEngineEventBus, EngineEventBus>();
        services.AddSingleton<IProcessingService, ProcessingService>();
        services.AddSingleton<IAIToolDispatcher, AIToolDispatcher>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IAppStatusService, AppStatusService>();
        services.AddSingleton<INavigationService, NavigationService>();

        return services;
    }
}
