namespace EngineShell.Application.Exceptions;

/// <summary>
/// Indicates that the configured AI provider could not be reached.
/// </summary>
public sealed class AIServiceUnavailableException : Exception
{
    public AIServiceUnavailableException(Exception innerException)
        : base("The AI service is unavailable.", innerException)
    {
    }
}
