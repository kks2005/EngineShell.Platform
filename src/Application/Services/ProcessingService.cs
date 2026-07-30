using Engine.Contracts;
using EngineShell.Application.Exceptions;
using EngineShell.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace EngineShell.Application.Services;

public sealed class ProcessingService : IProcessingService
{
    private readonly IProcessingEngine _engine;
    private readonly ILogger<ProcessingService> _logger;

    public ProcessingService(
        IProcessingEngine engine,
        ILogger<ProcessingService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engine = engine;
        _logger = logger ?? NullLogger<ProcessingService>.Instance;
    }

    public async Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var operationId = OperationId.Create();

        using var operationScope = _logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["OperationId"] = operationId,
                ["Engine"] = _engine.GetType().Name
            });

        if (request is null)
        {
            _logger.LogWarning(
                "Processing request rejected. Reason={Reason}",
                "RequestRequired");
            throw new ArgumentNullException(nameof(request));
        }

        using var requestScope = _logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["InputPath"] = request.InputPath,
                ["OutputPath"] = request.OutputPath
            });

        if (string.IsNullOrWhiteSpace(request.InputPath))
        {
            _logger.LogWarning(
                "Processing request rejected. Reason={Reason}",
                "InputPathRequired");
            throw new ArgumentException(
                "InputPath is required.",
                nameof(request));
        }

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Processing started.");

        try
        {
            var result = await _engine.ProcessAsync(
                request,
                progress,
                cancellationToken);

            result.OperationId = operationId;

            if (result.Success)
            {
                _logger.LogInformation(
                    "Processing completed in {ElapsedMilliseconds} ms. "
                    + "OutputPath={OutputPath}",
                    stopwatch.ElapsedMilliseconds,
                    result.OutputPath);
            }
            else
            {
                _logger.LogWarning(
                    "Processing failed in {ElapsedMilliseconds} ms with "
                    + "{ErrorCode}.",
                    stopwatch.ElapsedMilliseconds,
                    result.ErrorCode);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Processing cancelled after {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected processing failure after "
                + "{ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);

            throw new ProcessingOperationException(
                operationId,
                "An unexpected processing error occurred.",
                exception);
        }
    }

    public Task StopAsync()
    {
        _logger.LogWarning("StopAsync is not implemented.");
        throw new NotImplementedException();
    }
}
