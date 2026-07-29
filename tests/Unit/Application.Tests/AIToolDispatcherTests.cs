using Engine.Contracts;
using EngineShell.Application.AI;
using EngineShell.Application.Interfaces;
using EngineShell.Application.Services;
using Moq;

namespace Application.Tests;

[TestClass]
public sealed class AIToolDispatcherTests
{
    [TestMethod]
    public async Task ExecuteAsync_ProcessFile_UsesProcessingService()
    {
        var processingService = new Mock<IProcessingService>();
        processingService
            .Setup(service => service.ProcessAsync(
                new ProcessingRequest("input.dat", "output.dat"),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessingResult(true, "output.dat"));
        var sut = new AIToolDispatcher(processingService.Object);

        var result = await sut.ExecuteAsync(
            new AIToolCall(
                AIToolNames.ProcessFile,
                "input.dat",
                "output.dat"));

        Assert.AreEqual("Processing completed: output.dat", result);
        processingService.VerifyAll();
    }

    [TestMethod]
    public async Task ExecuteAsync_UnknownTool_IsRejected()
    {
        var sut = new AIToolDispatcher(Mock.Of<IProcessingService>());

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => sut.ExecuteAsync(
                new AIToolCall("delete_file", "input.dat")));
    }
}
