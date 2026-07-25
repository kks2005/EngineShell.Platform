using Engine.Contracts;

namespace Engine.Adapter.Simulator;
public sealed class SimulatorEngineAdapter : IProcessingEngine
{
    public Task<ProcessingResult> ProcessAsync(ProcessingRequest request, IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
