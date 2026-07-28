using Engine.Contracts;
using EngineShell.Application.Services;
using Presentation.ViewModels;

namespace Integration.Tests;

[TestClass]
public sealed class ProcessingWorkflowTests
{
    [TestMethod]
    public async Task ProcessCommand_FlowsThroughApplicationServiceToEngine()
    {
        // Arrange
        var engine = new RecordingEngine(
            new ProcessingResult(true, "rendered-output.dat"));
        var processingService = new ProcessingService(engine);
        using var eventBus = new EngineEventBus();
        using var sut = new RenderEngineViewModel(processingService, eventBus)
        {
            InputPath = "source-input.dat"
        };

        // Act
        await sut.ProcessCommand.ExecuteAsync(null);

        // Assert
        Assert.IsNotNull(engine.ReceivedRequest);
        Assert.AreEqual("source-input.dat", engine.ReceivedRequest.InputPath);
        Assert.AreEqual("Completed", sut.Status);
    }

    [TestMethod]
    public async Task CancelCommand_CancelsOperationAcrossAllLayers()
    {
        // Arrange
        var engine = new CancellableEngine();
        var processingService = new ProcessingService(engine);
        using var eventBus = new EngineEventBus();
        using var sut = new RenderEngineViewModel(processingService, eventBus)
        {
            InputPath = "source-input.dat"
        };

        // Act
        var processingTask = sut.ProcessCommand.ExecuteAsync(null);
        await engine.Started;
        sut.CancelCommand.Execute(null);
        await processingTask;

        // Assert
        Assert.IsTrue(engine.ObservedCancellation);
        Assert.AreEqual("Cancelled", sut.Status);
    }

    private sealed class RecordingEngine(ProcessingResult result) : IProcessingEngine
    {
        public ProcessingRequest? ReceivedRequest { get; private set; }

        public Task<ProcessingResult> ProcessAsync(
            ProcessingRequest request,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            progress?.Report(new ProcessingProgress(100, "Engine completed"));
            return Task.FromResult(result);
        }
    }

    private sealed class CancellableEngine : IProcessingEngine
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;
        public bool ObservedCancellation { get; private set; }

        public async Task<ProcessingResult> ProcessAsync(
            ProcessingRequest request,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _started.SetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ObservedCancellation = true;
                throw;
            }

            return new ProcessingResult(true, "unreachable");
        }
    }
}
