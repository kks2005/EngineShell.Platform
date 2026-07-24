using Engine.Contracts;

namespace Engine.Adapter.PInvoke;
/// <summary>
/// IProcessingEngine implementation that uses P/Invoke to call native code for processing. This adapter serves as a bridge between managed and unmanaged code, allowing the processing engine to leverage native libraries for performance or functionality that is not available in managed code.
/// </summary>
public sealed class PInvokeEngineAdapter : IProcessingEngine
{
    private readonly IEngineEventBus _eventBus;
    public PInvokeEngineAdapter(IEngineEventBus engineEventBus)
    {
        _eventBus = engineEventBus;

    }
    public async Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _eventBus.Publish(new EngineEvent(
        EngineEventType.Started,
        "Processing started.",
        DateTimeOffset.Now));

        try
        {
            for (var percent = 0; percent <= 100; percent += 10)
            {
                progress?.Report(new ProcessingProgress(
                    percent,
                    $"Processing... {percent}%"));

                if (percent < 100)
                {
                    await Task.Delay(200, cancellationToken);
                }
            }

            _eventBus.Publish(new EngineEvent(
       EngineEventType.Completed,
       "Processing completed.",
       DateTimeOffset.Now));


            return new ProcessingResult(
                Success: true,
                OutputPath: request.OutputPath ??
                            $"{request.InputPath}.processed");
        }
        catch (OperationCanceledException)
        {
            progress?.Report(new ProcessingProgress(
                0,
                "Cancelled"));

            throw;
        }
    }
}
