using AI.Adapter.Ollama;
using EngineShell.Application.AI;
using EngineShell.Application.Exceptions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Integration.Tests;

[TestClass]
public sealed class OllamaAIServiceTests
{
    [TestMethod]
    public async Task LocalHost_WhenApiIsAvailable_DoesNotStartAnotherServer()
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434/")
        };
        var sut = new OllamaLocalHost(httpClient);

        var isRunning = await sut.EnsureRunningAsync();

        Assert.IsTrue(isRunning);
    }

    [TestMethod]
    public async Task LocalHost_RejectsNonLocalEndpoint()
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://example.com/")
        };
        var sut = new OllamaLocalHost(httpClient);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => sut.EnsureRunningAsync());
    }

    [TestMethod]
    public async Task PlanAsync_MapsStructuredProcessFileResponse()
    {
        var handler = new StubHandler(request =>
        {
            Assert.AreEqual(
                new Uri("http://localhost:11434/api/chat"),
                request.RequestUri);

            var plan = JsonSerializer.Serialize(new
            {
                action = "process_file",
                message = "I will process the file.",
                inputPath = "input.dat",
                outputPath = (string?)null
            });
            var response = JsonSerializer.Serialize(new
            {
                message = new
                {
                    role = "assistant",
                    content = plan
                }
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    response,
                    Encoding.UTF8,
                    "application/json")
            };
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434/")
        };
        var sut = new OllamaAIService(httpClient, "test-model");

        var result = await sut.PlanAsync("process input.dat");

        Assert.AreEqual("I will process the file.", result.Message);
        Assert.IsNotNull(result.ToolCall);
        Assert.AreEqual(AIToolNames.ProcessFile, result.ToolCall.Name);
        Assert.AreEqual("input.dat", result.ToolCall.InputPath);
    }

    [TestMethod]
    public async Task PlanAsync_WhenProviderCannotBeReached_MapsNeutralFailure()
    {
        var transportFailure = new HttpRequestException(
            "Provider-specific transport failure.");
        var handler = new StubHandler(_ => throw transportFailure);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434/")
        };
        var sut = new OllamaAIService(httpClient, "test-model");

        var exception =
            await Assert.ThrowsExceptionAsync<AIServiceUnavailableException>(
                () => sut.PlanAsync("hello"));

        Assert.AreSame(transportFailure, exception.InnerException);
        Assert.IsFalse(exception.Message.Contains(
            "Ollama",
            StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
