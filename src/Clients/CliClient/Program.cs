using Engine.Adapter.Simulator;
using Engine.Contracts;
using EngineShell.Application;
using EngineShell.Application.Interfaces;
using CliClient;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddApplication();
services.AddSingleton<IProcessingEngine, SimulatorEngineAdapter>();

await using var provider = services.BuildServiceProvider();
using var cancellation = new CancellationTokenSource();

ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

Console.CancelKeyPress += cancelHandler;

try
{
    var runner = new CliRunner(
        provider.GetRequiredService<IProcessingService>(),
        Console.Out,
        Console.Error);

    return await runner.RunAsync(args, cancellation.Token);
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}
