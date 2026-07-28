using Engine.Contracts;

namespace Engine.Adapter.Simulator;

// Pure managed simulator — no native code required.
//
// Useful for:
//   - Cross-platform targets where Engine.Native.dll is not available (MAUI on Android/iOS)
//   - Unit and integration tests that need deterministic, fast processing
//   - CI environments without the native build toolchain
public sealed class SimulatorEngineAdapter : IProcessingEngine
{
    public async Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        for (int percent = 0; percent <= 100; percent += 10)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ProcessingProgress(percent, $"Simulating... {percent}%"));

            if (percent < 100)
                await Task.Delay(150, cancellationToken);
        }

        return new ProcessingResult(
            true,
            request.OutputPath ?? $"{request.InputPath}.simulated");
    }
}
