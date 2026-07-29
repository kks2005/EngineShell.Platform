using Engine.Contracts;
using EngineShell.Application.AI;
using EngineShell.Application.Interfaces;

namespace EngineShell.Application.Services;

/// <summary>
/// The allowlisted boundary between AI-generated plans and application code.
/// </summary>
public sealed class AIToolDispatcher(IProcessingService processingService)
    : IAIToolDispatcher
{
    public async Task<string> ExecuteAsync(
        AIToolCall toolCall,
        IProgress<ChatProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        if (!string.Equals(
                toolCall.Name,
                AIToolNames.ProcessFile,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"AI tool '{toolCall.Name}' is not allowed.");
        }

        if (string.IsNullOrWhiteSpace(toolCall.InputPath))
        {
            throw new ArgumentException(
                "The process_file tool requires an input path.");
        }

        var request = new ProcessingRequest(
            toolCall.InputPath,
            toolCall.OutputPath);

        var processingProgress = new Progress<ProcessingProgress>(update =>
            progress?.Report(new ChatProgress(
                update.Message ?? "Processing...",
                update.PercentComplete)));

        var result = await processingService.ProcessAsync(
            request,
            processingProgress,
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                result.ErrorMessage ?? "Processing failed.");
        }

        return $"Processing completed: {result.OutputPath}";
    }
}
