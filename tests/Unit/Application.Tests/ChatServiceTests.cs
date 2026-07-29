using EngineShell.Application.AI;
using EngineShell.Application.Interfaces;
using EngineShell.Application.Services;
using Moq;

namespace Application.Tests;

[TestClass]
public sealed class ChatServiceTests
{
    [TestMethod]
    public async Task SendAsync_ReplyPlan_ReturnsMessageWithoutToolExecution()
    {
        var aiService = new Mock<IAIService>();
        aiService
            .Setup(service => service.PlanAsync(
                "hello",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIPlan("Hello from Ollama."));
        var dispatcher = new Mock<IAIToolDispatcher>();
        var sut = new ChatService(
            aiService.Object,
            dispatcher.Object);

        var result = await sut.SendAsync("hello");

        Assert.AreEqual("Hello from Ollama.", result.Message);
        Assert.IsFalse(result.ToolExecuted);
        dispatcher.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SendAsync_ToolPlan_ExecutesAllowlistedTool()
    {
        var toolCall = new AIToolCall(
            AIToolNames.ProcessFile,
            "input.dat");
        var aiService = new Mock<IAIService>();
        aiService
            .Setup(service => service.PlanAsync(
                "process input.dat",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIPlan("I will process it.", toolCall));
        var dispatcher = new Mock<IAIToolDispatcher>();
        dispatcher
            .Setup(service => service.ExecuteAsync(
                toolCall,
                It.IsAny<IProgress<ChatProgress>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Processing completed: input.dat.processed");
        var sut = new ChatService(
            aiService.Object,
            dispatcher.Object);

        var result = await sut.SendAsync("process input.dat");

        Assert.IsTrue(result.ToolExecuted);
        Assert.AreEqual(
            "Processing completed: input.dat.processed",
            result.Message);
    }

    [TestMethod]
    public async Task SendAsync_CapabilitiesPrompt_ReturnsCatalogHelp()
    {
        var aiService = new Mock<IAIService>();
        var dispatcher = new Mock<IAIToolDispatcher>();
        var sut = new ChatService(
            aiService.Object,
            dispatcher.Object);

        var result = await sut.SendAsync(AIChatPrompts.WhatCanYouDo);

        Assert.AreEqual(AIChatPrompts.CapabilitiesHelp, result.Message);
        aiService.VerifyNoOtherCalls();
        dispatcher.VerifyNoOtherCalls();
    }
}
