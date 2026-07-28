using Engine.Contracts;

namespace Engine.Adapter.PInvoke;

/// <summary>
/// Maps the managed processing contract to the C API exposed by Engine.Native.
/// </summary>
public sealed class PInvokeEngineAdapter : IProcessingEngine
{
    private readonly IEngineEventBus _eventBus;

    public PInvokeEngineAdapter(IEngineEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _eventBus.Publish(new EngineEvent(
            EngineEventType.Started,
            "Processing started.",
            DateTimeOffset.Now));

        try
        {
            // Engine_Process is synchronous. Only the native work runs on the
            // worker thread; managed events are published on the caller context.
            var result = await Task.Run(
                () => ProcessNative(request, progress, cancellationToken),
                cancellationToken);

            _eventBus.Publish(new EngineEvent(
                result.Success
                    ? EngineEventType.Completed
                    : EngineEventType.Error,
                result.Success
                    ? "Processing completed."
                    : result.ErrorMessage ?? "Processing failed.",
                DateTimeOffset.Now));

            return result;
        }
        catch (OperationCanceledException)
        {
            progress?.Report(new ProcessingProgress(0, "Cancelled"));

            _eventBus.Publish(new EngineEvent(
                EngineEventType.Cancelled,
                "Processing cancelled.",
                DateTimeOffset.Now));

            throw;
        }
    }

    private static ProcessingResult ProcessNative(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress,
        CancellationToken cancellationToken)
    {
        var nativeRequest = new NativeMethods.EngineRequestDto
        {
            InputPath = request.InputPath,
            OutputPath = request.OutputPath
        };

        ProcessingResult? completedResult = null;

        NativeMethods.ProgressCallback onProgress =
            (percent, message, _) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    NativeMethods.Engine_Cancel();
                    return;
                }

                progress?.Report(new ProcessingProgress(
                    percent,
                    NativeMethods.ToManagedString(message)));
            };

        NativeMethods.CompletionCallback onCompletion =
            (success, outputPath, _) =>
            {
                completedResult = new ProcessingResult(
                    success == 1,
                    NativeMethods.ToManagedString(outputPath));
            };

        cancellationToken.ThrowIfCancellationRequested();

        using var cancellationRegistration =
            cancellationToken.Register(NativeMethods.Engine_Cancel);

        var status = NativeMethods.Engine_Process(
            ref nativeRequest,
            onProgress,
            onCompletion,
            nint.Zero);

        // Keep callback delegates alive until the synchronous native call ends.
        GC.KeepAlive(onProgress);
        GC.KeepAlive(onCompletion);

        if (status == -1)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (status != 0 || completedResult is null)
        {
            var message = $"Native processing failed with code {status}.";
            return new ProcessingResult(false, null, message);
        }

        return completedResult;
    }
}
