using Engine.Adapter.PInvoke;
using Engine.Adapter.Simulator;
using Engine.Contracts;
using EngineShell.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Presentation;
using Presentation.Dialogs;
using Clients.Maui.Services;

namespace Clients.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddApplication();
        builder.Services.AddSingleton<IDialogService, MauiDialogService>();
        builder.Services.AddPresentation();

#if WINDOWS
        builder.Services.AddSingleton<IProcessingEngine, PInvokeEngineAdapter>();
#else
        builder.Services.AddSingleton<IProcessingEngine, SimulatorEngineAdapter>();
#endif

        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
