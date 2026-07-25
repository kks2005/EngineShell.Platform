using Engine.Contracts;
using EngineShell.Application.Interfaces;
using EngineShell.Application.Models;
using EngineShell.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Presentation.ViewModels;

namespace Headless.Tests;

[TestClass]
public sealed class HeadlessWorkflowTests
{
    [TestMethod]
    public async Task UserCanNavigateToRenderEngineAndCompleteProcessing()
    {
        // Arrange
        var engine = new ControllableEngine
        {
            Result = new ProcessingResult(true, "rendered.dat")
        };
        await using var host = HeadlessTestHost.Create(engine);
        var shell = host.Services.GetRequiredService<ShellViewModel>();
        var renderItem = shell.Header.NavigationItems.Single(
            item => item.Title == "RenderEngine");

        // Act
        shell.Header.SelectedNavigationItem = renderItem;
        var renderViewModel = (RenderEngineViewModel)shell.CurrentViewModel;
        renderViewModel.InputPath = "scene.dat";
        await renderViewModel.ProcessCommand.ExecuteAsync(null);

        // Assert
        Assert.AreEqual("scene.dat", engine.ReceivedRequest?.InputPath);
        Assert.AreEqual("Completed", renderViewModel.Status);
        Assert.AreEqual("Loaded RenderEngine", shell.Status.Status);
        Assert.AreEqual(0d, shell.Status.Progress);
    }

    [TestMethod]
    public async Task EngineFailureIsReportedThroughTheHeadlessWorkflow()
    {
        // Arrange
        var engine = new ControllableEngine
        {
            Result = new ProcessingResult(
                false,
                null,
                "The scene could not be rendered.")
        };
        await using var host = HeadlessTestHost.Create(engine);
        var shell = host.Services.GetRequiredService<ShellViewModel>();
        var renderItem = shell.Header.NavigationItems.Single(
            item => item.Title == "RenderEngine");

        // Act
        shell.Header.SelectedNavigationItem = renderItem;
        var renderViewModel = (RenderEngineViewModel)shell.CurrentViewModel;
        renderViewModel.InputPath = "invalid-scene.dat";
        await renderViewModel.ProcessCommand.ExecuteAsync(null);

        // Assert
        Assert.AreEqual("invalid-scene.dat", engine.ReceivedRequest?.InputPath);
        Assert.AreEqual(
            "The scene could not be rendered.",
            renderViewModel.Status);
    }

    [TestMethod]
    public async Task UserCanCancelProcessingFromTheHeadlessWorkflow()
    {
        // Arrange
        var engine = new ControllableEngine { WaitForCancellation = true };
        await using var host = HeadlessTestHost.Create(engine);
        var renderViewModel =
            host.Services.GetRequiredService<RenderEngineViewModel>();
        renderViewModel.InputPath = "long-running-scene.dat";

        // Act
        var processingTask = renderViewModel.ProcessCommand.ExecuteAsync(null);
        await engine.Started;
        renderViewModel.CancelCommand.Execute(null);
        await processingTask;

        // Assert
        Assert.IsTrue(engine.ObservedCancellation);
        Assert.AreEqual("Cancelled", renderViewModel.Status);
    }

    private sealed class HeadlessTestHost : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        private HeadlessTestHost(ServiceProvider provider)
        {
            _provider = provider;
        }

        public IServiceProvider Services => _provider;

        public static HeadlessTestHost Create(IProcessingEngine engine)
        {
            var services = new ServiceCollection();

            services.AddSingleton(engine);
            services.AddSingleton<IProcessingEngine>(engine);
            services.AddSingleton<IEngineEventBus, EngineEventBus>();
            services.AddSingleton<IProcessingService, ProcessingService>();
            services.AddSingleton<IAppStatusService, AppStatusService>();
            services.AddSingleton<GeneralViewModel>();
            services.AddSingleton<RenderEngineViewModel>();
            services.AddSingleton<FooterViewModel>();
            services.AddSingleton(
                sp => new NavigationItem(
                    "General",
                    "home",
                    sp.GetRequiredService<GeneralViewModel>()));
            services.AddSingleton(
                sp => new NavigationItem(
                    "RenderEngine",
                    "render",
                    sp.GetRequiredService<RenderEngineViewModel>()));
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<HeaderViewModel>();
            services.AddSingleton<ShellViewModel>();

            return new HeadlessTestHost(services.BuildServiceProvider());
        }

        public ValueTask DisposeAsync()
        {
            return _provider.DisposeAsync();
        }
    }

    private sealed class ControllableEngine : IProcessingEngine
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ProcessingRequest? ReceivedRequest { get; private set; }
        public ProcessingResult Result { get; init; } =
            new(true, "output.dat");
        public bool WaitForCancellation { get; init; }
        public bool ObservedCancellation { get; private set; }
        public Task Started => _started.Task;

        public async Task<ProcessingResult> ProcessAsync(
            ProcessingRequest request,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            _started.TrySetResult();

            if (!WaitForCancellation)
            {
                return Result;
            }

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ObservedCancellation = true;
                throw;
            }

            return Result;
        }
    }
}
