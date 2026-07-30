namespace Engine.Contracts;

/// <summary>
/// Defines the shared asynchronous contract implemented by processing engines.
/// </summary>
public interface IProcessingEngine
{
    Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public record ProcessingRequest(
    string InputPath,
    string? OutputPath = null);

public sealed class ProcessingResult
{
    public ProcessingResult()
    {
    }

    public ProcessingResult(
        bool success,
        string? outputPath,
        string? errorMessage = null)
    {
        Success = success;
        OutputPath = outputPath;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; private set; }
    public string? OutputPath { get; private set; }
    public string? ErrorMessage { get; private set; }
    public ProcessingErrorCode ErrorCode { get; set; }
    public string? OperationId { get; set; }
}

public enum ProcessingErrorCode
{
    None,
    InvalidRequest,
    NativeProcessingFailed
}

public record ProcessingProgress(
    int PercentComplete,
    string? Message = null);
