namespace Engine.Adapter.PInvoke;

/// <summary>
/// Adds managed/native boundary context without losing the original exception.
/// </summary>
public sealed class EngineInteropException(
    string message,
    Exception innerException)
    : Exception(message, innerException);
