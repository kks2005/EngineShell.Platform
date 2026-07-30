namespace EngineShell.Application.Exceptions;

/// <summary>
/// Carries a user-visible operation reference while preserving the original
/// technical exception as InnerException.
/// </summary>
public sealed class ProcessingOperationException : Exception
{
    public ProcessingOperationException(
        string operationId,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        OperationId = operationId;
    }

    public ProcessingOperationException(
        string operationId,
        string message)
        : base(message)
    {
        OperationId = operationId;
    }

    public string OperationId { get; }
}
