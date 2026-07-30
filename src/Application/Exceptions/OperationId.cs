namespace EngineShell.Application.Exceptions;

public static class OperationId
{
    public static string Create() =>
        Guid.NewGuid().ToString("N")[..8];
}
