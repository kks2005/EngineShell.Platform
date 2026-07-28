namespace Engine.Contracts;

/// <summary>
/// Interface for a processing engine that defines a contract for processing requests and returning results asynchronously.
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

    public ProcessingResult(bool success, string? outputPath, string? errorMessage = null)
    {
        Success = success;
        OutputPath = outputPath;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
}

public record ProcessingProgress(
    int PercentComplete,
    string? Message = null);