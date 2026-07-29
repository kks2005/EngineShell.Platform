using System.Diagnostics;

namespace AI.Adapter.Ollama;

/// <summary>
/// Checks and starts the locally installed Ollama server.
/// </summary>
public sealed class OllamaLocalHost(HttpClient httpClient)
{
    public async Task<bool> EnsureRunningAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureLocalAddress();

        if (await IsRunningAsync(cancellationToken))
        {
            return true;
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "ollama",
            Arguments = "serve",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        if (process is null)
        {
            return false;
        }

        // Ollama may need a few seconds before its HTTP endpoint is ready.
        for (var attempt = 0; attempt < 15; attempt++)
        {
            await Task.Delay(300, cancellationToken);

            if (await IsRunningAsync(cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsRunningAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                "api/tags",
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private void EnsureLocalAddress()
    {
        if (httpClient.BaseAddress is not { IsLoopback: true })
        {
            throw new InvalidOperationException(
                "This POC supports only a local Ollama endpoint.");
        }
    }
}
