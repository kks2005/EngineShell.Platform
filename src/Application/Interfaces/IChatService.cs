using EngineShell.Application.AI;

namespace EngineShell.Application.Interfaces;

public interface IChatService
{
    Task<ChatResult> SendAsync(
        string userMessage,
        IProgress<ChatProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
