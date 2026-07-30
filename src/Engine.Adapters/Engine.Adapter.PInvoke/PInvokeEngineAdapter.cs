using Engine.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.InteropServices;

namespace Engine.Adapter.PInvoke;

/// <summary>
/// Maps the managed processing contract to the C API exposed by Engine.Native.
/// </summary>
public sealed class PInvokeEngineAdapter : IProcessingEngine
{
    private readonly IEngineEventBus _eventBus;
    private readonly ILogger<PInvokeEngineAdapter> _logger;

    public PInvokeEngineAdapter(
        IEngineEventBus eventBus,
        ILogger<PInvokeEngineAdapter>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        _eventBus = eventBus;
        _logger = logger ?? NullLogger<PInvokeEngineAdapter>.Instance;
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
        catch (Exception exception) when (
            exception is DllNotFoundException
                or EntryPointNotFoundException
                or BadImageFormatException
                or MarshalDirectiveException
                or SEHException)
        {
            throw new EngineInteropException(
                "The Engine.Native processing call failed.",
                exception);
        }
    }

    private ProcessingResult ProcessNative(
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

        _logger.LogDebug(
            "Calling Engine_Process. OutputPath={OutputPath}",
            request.OutputPath);

        var status = NativeMethods.Engine_Process(
            ref nativeRequest,
            onProgress,
            onCompletion,
            nint.Zero);

        _logger.LogDebug(
            "Engine_Process returned NativeStatus={NativeStatus}.",
            status);

        // Keep callback delegates alive until the synchronous native call ends.
        GC.KeepAlive(onProgress);
        GC.KeepAlive(onCompletion);

        if (status == NativeMethods.Cancelled)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (status != NativeMethods.Success || completedResult is null)
        {
            var errorCode = status == NativeMethods.InvalidRequest
                ? ProcessingErrorCode.InvalidRequest
                : ProcessingErrorCode.NativeProcessingFailed;
            var message = status == NativeMethods.InvalidRequest
                ? "The processing request is invalid."
                : "The native engine could not process the file.";

            return new ProcessingResult(false, null, message)
            {
                ErrorCode = errorCode
            };
        }

        if (!completedResult.Success)
        {
            completedResult.ErrorCode =
                ProcessingErrorCode.NativeProcessingFailed;
        }

        return completedResult;
    }
}
