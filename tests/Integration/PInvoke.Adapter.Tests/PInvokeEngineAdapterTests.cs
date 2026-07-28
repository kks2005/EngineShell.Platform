using Engine.Adapter.PInvoke;
using Engine.Contracts;

namespace PInvoke.Adapter.Tests;

[TestClass]
public sealed class PInvokeEngineAdapterTests
{
    [TestMethod]
    [Description("Verifies that ProcessAsync reports progress, returns a result, and publishes lifecycle events.")]
    [TestCategory("Integration")]
    public async Task ProcessAsync_ReportsProgressResultAndLifecycleEvents()
    {
        using var eventBus = new EngineEventBus();
        var events = new List<EngineEvent>();
        using var subscription = eventBus.Events.Subscribe(events.Add);
        var progress = new RecordingProgress();
        var sut = new PInvokeEngineAdapter(eventBus);

        var result = await sut.ProcessAsync(
            new ProcessingRequest("input.dat"),
            progress);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("input.dat.processed", result.OutputPath);
        CollectionAssert.Contains(progress.Values.ToArray(), 0);
        CollectionAssert.Contains(progress.Values.ToArray(), 100);
        CollectionAssert.Contains(
            events.Select(item => item.Type).ToArray(),
            EngineEventType.Started);
        CollectionAssert.Contains(
            events.Select(item => item.Type).ToArray(),
            EngineEventType.Completed);
    }

    [TestMethod]
    [Description("Verifies that ProcessAsync preserves the explicit output path specified in the request.")]
    [TestCategory("Integration")]
    public async Task ProcessAsync_PreservesExplicitOutputPath()
    {
        using var eventBus = new EngineEventBus();
        var sut = new PInvokeEngineAdapter(eventBus);

        var result = await sut.ProcessAsync(
            new ProcessingRequest("input.dat", "output.dat"));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("output.dat", result.OutputPath);
    }

    [TestMethod]
    [Description("Verifies that ProcessAsync cancels the native operation when the cancellation token is triggered.")]
    [TestCategory("Integration")]
    public async Task ProcessAsync_CancelsNativeOperation()
    {
        using var eventBus = new EngineEventBus();
        var events = new List<EngineEvent>();
        using var subscription = eventBus.Events.Subscribe(events.Add);
        var progress = new RecordingProgress();
        using var cancellation = new CancellationTokenSource();
        var sut = new PInvokeEngineAdapter(eventBus);

        var processing = sut.ProcessAsync(
            new ProcessingRequest("input.dat"),
            progress,
            cancellation.Token);

        await progress.FirstReport;
        await cancellation.CancelAsync();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            async () => await processing);
        CollectionAssert.Contains(
            events.Select(item => item.Type).ToArray(),
            EngineEventType.Cancelled);
    }

    private sealed class RecordingProgress : IProgress<ProcessingProgress>
    {
        private readonly TaskCompletionSource _firstReport =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<int> Values { get; } = [];
        public Task FirstReport => _firstReport.Task;

        public void Report(ProcessingProgress value)
        {
            Values.Add(value.PercentComplete);
            _firstReport.TrySetResult();
        }
    }
}
