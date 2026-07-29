namespace EngineShell.Application.AI;

public static class AIToolNames
{
    public const string ProcessFile = "process_file";
}

public sealed record AIPlan(
    string Message,
    AIToolCall? ToolCall = null);

public sealed record AIToolCall(
    string Name,
    string InputPath,
    string? OutputPath = null);

public sealed record ChatResult(
    string Message,
    bool ToolExecuted = false);

public sealed record ChatProgress(
    string Message,
    int? PercentComplete = null);
