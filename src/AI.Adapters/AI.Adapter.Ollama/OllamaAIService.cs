using EngineShell.Application.AI;
using EngineShell.Application.Interfaces;
using EngineShell.Application.Exceptions;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AI.Adapter.Ollama;

/// <summary>
/// Uses Ollama structured output to create a small, validated application plan.
/// </summary>
public sealed class OllamaAIService(
    HttpClient httpClient,
    string model) : IAIService
{
    private const string SystemPrompt =
        """
        You are an assistant for EngineShell.
        Use action "process_file" only when the user asks to process a file.
        Otherwise use action "reply".
        Never invent an input path.
        Return a short, user-friendly message.

        EngineShell processing flows through:
        Client UI -> ChatService -> AIToolDispatcher -> ProcessingService ->
        IProcessingEngine -> managed/native adapter -> Engine.Native C++.
        The adapter maps managed requests and native progress/results.
        The application validates and executes tools; you only propose them.
        """;

    private static readonly JsonElement ResponseSchema =
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                action = new
                {
                    type = "string",
                    @enum = new[] { "reply", "process_file" }
                },
                message = new { type = "string" },
                inputPath = new { type = new[] { "string", "null" } },
                outputPath = new { type = new[] { "string", "null" } }
            },
            required = new[]
            {
                "action",
                "message",
                "inputPath",
                "outputPath"
            }
        });

    public async Task<AIPlan> PlanAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("A chat message is required.");
        }

        var request = new OllamaChatRequest(
            model,
            [
                new("system", SystemPrompt),
                new("user", userMessage)
            ],
            ResponseSchema);

        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsJsonAsync(
                "api/chat",
                request,
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new AIServiceUnavailableException(exception);
        }

        using (response)
        {
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException exception)
            {
                throw new AIServiceUnavailableException(exception);
            }

            var chatResponse =
                await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
                    cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException(
                    "Ollama returned an empty response.");

            var plan = JsonSerializer.Deserialize<StructuredPlan>(
                chatResponse.Message.Content,
                JsonOptions)
                ?? throw new InvalidOperationException(
                    "Ollama returned an invalid plan.");

            return MapPlan(plan);
        }
    }

    private static AIPlan MapPlan(StructuredPlan plan)
    {
        if (plan.Action == "reply")
        {
            return new AIPlan(plan.Message);
        }

        if (plan.Action != AIToolNames.ProcessFile)
        {
            throw new InvalidOperationException(
                $"Ollama returned unsupported action '{plan.Action}'.");
        }

        if (string.IsNullOrWhiteSpace(plan.InputPath))
        {
            throw new InvalidOperationException(
                "Ollama did not provide the required input path.");
        }

        return new AIPlan(
            plan.Message,
            new AIToolCall(
                AIToolNames.ProcessFile,
                plan.InputPath,
                plan.OutputPath));
    }

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private sealed record OllamaChatRequest(
        string Model,
        IReadOnlyList<OllamaMessage> Messages,
        JsonElement Format)
    {
        [JsonPropertyName("stream")]
        public bool Stream => false;

        [JsonPropertyName("options")]
        public object Options => new { temperature = 0 };
    }

    private sealed record OllamaMessage(
        string Role,
        string Content);

    private sealed record OllamaChatResponse(
        OllamaMessage Message);

    private sealed record StructuredPlan(
        string Action,
        string Message,
        string? InputPath,
        string? OutputPath);
}
