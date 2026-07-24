using Engine.Contracts;
using EngineShell.Application.Interfaces;

namespace EngineShell.Application.Services;
public class ProcessingService : IProcessingService
{
    private readonly IProcessingEngine _engine;

    public ProcessingService(IProcessingEngine engine) => _engine = engine;

    public Task<ProcessingResult> ProcessAsync(ProcessingRequest request, IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.InputPath))
            throw new ArgumentException("InputPath is required.");

        return _engine.ProcessAsync(
           request,
           progress,
           cancellationToken);
    }

    public Task StopAsync()
    {
        throw new NotImplementedException();
    }
}


