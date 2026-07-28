using Engine.Adapter.CppCli;
using Engine.Contracts;

namespace CppCli.Adapter.Tests;

[TestClass]
public sealed class CppCliEngineAdapterTests
{
    [TestMethod]
    public async Task ProcessAsync_ReportsProgressResultAndLifecycleEvents()
    {
        using var eventBus = new EngineEventBus();
        var events = new List<EngineEvent>();
        using var subscription = eventBus.Events.Subscribe(events.Add);
        var progress = new RecordingProgress();
        var sut = new CppCliEngineAdapter(eventBus);

        var result = await sut.ProcessAsync(
            new ProcessingRequest("input.dat"),
            progress,
            CancellationToken.None);

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
    public async Task ProcessAsync_PreservesExplicitOutputPath()
    {
        using var eventBus = new EngineEventBus();
        var sut = new CppCliEngineAdapter(eventBus);

        var result = await sut.ProcessAsync(
            new ProcessingRequest("input.dat", "output.dat"),
            null,
            CancellationToken.None);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("output.dat", result.OutputPath);
    }

    [TestMethod]
    public async Task ProcessAsync_CancelsNativeOperation()
    {
        using var eventBus = new EngineEventBus();
        var events = new List<EngineEvent>();
        using var subscription = eventBus.Events.Subscribe(events.Add);
        var progress = new RecordingProgress();
        using var cancellation = new CancellationTokenSource();
        var sut = new CppCliEngineAdapter(eventBus);

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
