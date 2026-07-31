using Engine.Contracts;
using EngineShell.Application.Exceptions;
using EngineShell.Application.Interfaces;

namespace CliClient;

public sealed class CliRunner(
    IProcessingService processingService,
    TextWriter output,
    TextWriter error)
{
    public const int SuccessExitCode = 0;
    public const int ProcessingFailedExitCode = 1;
    public const int UsageErrorExitCode = 2;
    public const int CancelledExitCode = 3;
    public const int UnexpectedErrorExitCode = 4;

    public async Task<int> RunAsync(
        string[] args,
        CancellationToken cancellationToken = default)
    {
        var options = Parse(args);
        if (options is null)
        {
            await error.WriteLineAsync(
                "Usage: CliClient process <input> [--output <path>]");
            return UsageErrorExitCode;
        }

        await output.WriteLineAsync($"Processing: {options.InputPath}");

        var progress = new ConsoleProgress<ProcessingProgress>(
            update => output.WriteLine(
                $"Progress: {update.PercentComplete}% {update.Message}"));

        try
        {
            var result = await processingService.ProcessAsync(
                new ProcessingRequest(
                    options.InputPath,
                    options.OutputPath),
                progress,
                cancellationToken);

            if (!result.Success)
            {
                await error.WriteLineAsync(
                    $"Failed: {result.ErrorMessage ?? "Processing failed."}");
                await WriteOperationReferenceAsync(result.OperationId);
                return ProcessingFailedExitCode;
            }

            await output.WriteLineAsync($"Completed: {result.OutputPath}");
            await WriteOperationReferenceAsync(result.OperationId);
            return SuccessExitCode;
        }
        catch (OperationCanceledException)
        {
            await error.WriteLineAsync("Processing cancelled.");
            return CancelledExitCode;
        }
        catch (ProcessingOperationException exception)
        {
            await error.WriteLineAsync(exception.Message);
            await WriteOperationReferenceAsync(exception.OperationId);
            return UnexpectedErrorExitCode;
        }
        catch (Exception)
        {
            await error.WriteLineAsync(
                "An unexpected error occurred before processing completed.");
            return UnexpectedErrorExitCode;
        }
    }

    private async Task WriteOperationReferenceAsync(string? operationId)
    {
        if (!string.IsNullOrWhiteSpace(operationId))
        {
            await output.WriteLineAsync($"Operation: {operationId}");
        }
    }

    private static CommandOptions? Parse(string[] args)
    {
        if (args.Length is < 2 or > 4
            || !string.Equals(
                args[0],
                "process",
                StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(args[1]))
        {
            return null;
        }

        if (args.Length == 2)
        {
            return new CommandOptions(args[1], null);
        }

        if (args.Length != 4
            || !string.Equals(
                args[2],
                "--output",
                StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(args[3]))
        {
            return null;
        }

        return new CommandOptions(args[1], args[3]);
    }

    private sealed record CommandOptions(
        string InputPath,
        string? OutputPath);

    private sealed class ConsoleProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
