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

public record ProcessingResult(
    bool Success,
    string? OutputPath,
    string? ErrorMessage = null);

public record ProcessingProgress(
    int PercentComplete,
    string? Message = null);

public interface IEngineEventBus
{
    IObservable<EngineEvent> Events { get; }

    void Publish(EngineEvent engineEvent);
}
public sealed record EngineEvent(
    EngineEventType Type,
    string Message,
    DateTimeOffset Timestamp);

public enum EngineEventType
{
    Info,
    Started,
    Progress,
    Completed,
    Cancelled,
    Warning,
    Error
}
