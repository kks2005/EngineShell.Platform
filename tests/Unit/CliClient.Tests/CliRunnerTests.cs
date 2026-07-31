using CliClient;
using Engine.Contracts;
using EngineShell.Application.Interfaces;
using Moq;

namespace CliClient.Tests;

[TestClass]
public sealed class CliRunnerTests
{
    [TestMethod]
    public async Task ProcessCommand_ReportsProgressAndResult()
    {
        var service = new Mock<IProcessingService>();
        service
            .Setup(x => x.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProcessingRequest, IProgress<ProcessingProgress>?, CancellationToken>(
                (_, progress, _) =>
                    progress?.Report(
                        new ProcessingProgress(50, "Processing")))
            .ReturnsAsync(new ProcessingResult(true, "rendered.dat")
            {
                OperationId = "abc12345"
            });
        using var output = new StringWriter();
        using var error = new StringWriter();
        var sut = new CliRunner(service.Object, output, error);

        var exitCode = await sut.RunAsync(
            ["process", "scene.dat", "--output", "rendered.dat"]);

        Assert.AreEqual(CliRunner.SuccessExitCode, exitCode);
        service.Verify(x => x.ProcessAsync(
            It.Is<ProcessingRequest>(request =>
                request.InputPath == "scene.dat"
                && request.OutputPath == "rendered.dat"),
            It.IsAny<IProgress<ProcessingProgress>>(),
            It.IsAny<CancellationToken>()));
        StringAssert.Contains(output.ToString(), "Progress: 50%");
        StringAssert.Contains(output.ToString(), "Completed: rendered.dat");
        StringAssert.Contains(output.ToString(), "Operation: abc12345");
        Assert.AreEqual(string.Empty, error.ToString());
    }

    [TestMethod]
    public async Task MissingArguments_ReturnsUsageError()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var sut = new CliRunner(
            Mock.Of<IProcessingService>(),
            output,
            error);

        var exitCode = await sut.RunAsync([]);

        Assert.AreEqual(CliRunner.UsageErrorExitCode, exitCode);
        StringAssert.Contains(error.ToString(), "Usage: CliClient");
    }

    [TestMethod]
    public async Task ProcessingFailure_ReturnsStableExitCodeAndReference()
    {
        var service = new Mock<IProcessingService>();
        service
            .Setup(x => x.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessingResult(
                false,
                null,
                "Input could not be processed.")
            {
                OperationId = "def67890"
            });
        using var output = new StringWriter();
        using var error = new StringWriter();
        var sut = new CliRunner(service.Object, output, error);

        var exitCode = await sut.RunAsync(
            ["process", "invalid.dat"]);

        Assert.AreEqual(CliRunner.ProcessingFailedExitCode, exitCode);
        StringAssert.Contains(
            error.ToString(),
            "Input could not be processed.");
        StringAssert.Contains(output.ToString(), "Operation: def67890");
    }

    [TestMethod]
    public async Task Cancellation_ReturnsStableExitCode()
    {
        var service = new Mock<IProcessingService>();
        service
            .Setup(x => x.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        using var output = new StringWriter();
        using var error = new StringWriter();
        var sut = new CliRunner(service.Object, output, error);

        var exitCode = await sut.RunAsync(
            ["process", "scene.dat"]);

        Assert.AreEqual(CliRunner.CancelledExitCode, exitCode);
        StringAssert.Contains(error.ToString(), "cancelled");
    }
}
