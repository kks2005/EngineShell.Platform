using Engine.Contracts;

namespace EngineShell.Application.Interfaces;

/// <summary>
/// Interface for a processing service that defines a contract for processing requests and returning results asynchronously.
/// </summary>
public interface IProcessingService
{
    /// <summary>
    /// Processes a given request asynchronously, providing progress updates and supporting cancellation.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="progress"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///  Stops the processing service asynchronously, allowing for any necessary cleanup or resource release.
    /// </summary>
    /// <returns></returns>
    Task StopAsync();
}



