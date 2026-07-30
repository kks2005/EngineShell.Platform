using Engine.Contracts;
using EngineShell.Application.Exceptions;
using EngineShell.Application.Services;
using Moq;

namespace Application.Tests;

[TestClass]
public sealed class ProcessingServiceTests
{
    [TestMethod]
    public async Task ProcessAsync_WithValidRequest_DelegatesToEngineAndReturnsResult()
    {
        // Arrange
        var request = new ProcessingRequest("input.dat", "output.dat");
        var progress = Mock.Of<IProgress<ProcessingProgress>>();
        using var cancellationSource = new CancellationTokenSource();
        var expectedResult = new ProcessingResult(true, "output.dat");
        var engine = new Mock<IProcessingEngine>();
        engine
            .Setup(x => x.ProcessAsync(request, progress, cancellationSource.Token))
            .ReturnsAsync(expectedResult);
        var sut = new ProcessingService(engine.Object);

        // Act
        var result = await sut.ProcessAsync(request, progress, cancellationSource.Token);

        // Assert
        Assert.AreSame(expectedResult, result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.OperationId));
        engine.Verify(
            x => x.ProcessAsync(request, progress, cancellationSource.Token),
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessAsync_WithoutOptionalArguments_DelegatesDefaultsToEngine()
    {
        // Arrange
        var request = new ProcessingRequest("input.dat");
        var expectedResult = new ProcessingResult(true, "output.dat");
        var engine = new Mock<IProcessingEngine>();
        engine
            .Setup(x => x.ProcessAsync(request, null, CancellationToken.None))
            .ReturnsAsync(expectedResult);
        var sut = new ProcessingService(engine.Object);

        // Act
        var result = await sut.ProcessAsync(request);

        // Assert
        Assert.AreSame(expectedResult, result);
        engine.Verify(
            x => x.ProcessAsync(request, null, CancellationToken.None),
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var engine = new Mock<IProcessingEngine>();
        var sut = new ProcessingService(engine.Object);

        // Act
        var exception =
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => sut.ProcessAsync(null!));

        // Assert
        Assert.AreEqual("request", exception.ParamName);
        engine.VerifyNoOtherCalls();
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public async Task ProcessAsync_WithBlankInputPath_ThrowsArgumentException(
        string inputPath)
    {
        // Arrange
        var engine = new Mock<IProcessingEngine>();
        var sut = new ProcessingService(engine.Object);
        var request = new ProcessingRequest(inputPath);

        // Act
        var exception =
            await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => sut.ProcessAsync(request));

        // Assert
        StringAssert.Contains(exception.Message, "InputPath is required.");
        engine.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProcessAsync_WhenEngineFails_PreservesExceptionAndAddsOperationId()
    {
        // Arrange
        var request = new ProcessingRequest("input.dat");
        var expectedException = new InvalidOperationException("Engine failed.");
        var engine = new Mock<IProcessingEngine>();
        engine
            .Setup(x => x.ProcessAsync(request, null, CancellationToken.None))
            .ThrowsAsync(expectedException);
        var sut = new ProcessingService(engine.Object);

        // Act
        var exception =
            await Assert.ThrowsExceptionAsync<ProcessingOperationException>(
            () => sut.ProcessAsync(request));

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(exception.OperationId));
        Assert.AreSame(expectedException, exception.InnerException);
    }

    [TestMethod]
    public async Task ProcessAsync_WithExpectedFailure_AddsOperationId()
    {
        var request = new ProcessingRequest("input.dat");
        var expectedResult = new ProcessingResult(
            false,
            null,
            "The file could not be processed.")
        {
            ErrorCode = ProcessingErrorCode.NativeProcessingFailed
        };
        var engine = new Mock<IProcessingEngine>();
        engine
            .Setup(x => x.ProcessAsync(
                request,
                null,
                CancellationToken.None))
            .ReturnsAsync(expectedResult);
        var sut = new ProcessingService(engine.Object);

        var result = await sut.ProcessAsync(request);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(
            ProcessingErrorCode.NativeProcessingFailed,
            result.ErrorCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.OperationId));
    }

    [TestMethod]
    public void StopAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var sut = new ProcessingService(Mock.Of<IProcessingEngine>());

        // Act
        Action action = () => sut.StopAsync();

        // Assert
        Assert.ThrowsException<NotImplementedException>(action);
    }
}
