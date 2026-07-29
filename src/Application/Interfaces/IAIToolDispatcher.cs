using EngineShell.Application.AI;

namespace EngineShell.Application.Interfaces;

/// <summary>
/// Executes only tool calls explicitly supported by the application.
/// </summary>
public interface IAIToolDispatcher
{
    Task<string> ExecuteAsync(
        AIToolCall toolCall,
        IProgress<ChatProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
