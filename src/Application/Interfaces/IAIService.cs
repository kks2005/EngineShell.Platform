using EngineShell.Application.AI;

namespace EngineShell.Application.Interfaces;

/// <summary>
/// Converts a natural-language request into a response and an optional,
/// structured application tool call.
/// </summary>
public interface IAIService
{
    Task<AIPlan> PlanAsync(
        string userMessage,
        CancellationToken cancellationToken = default);
}
