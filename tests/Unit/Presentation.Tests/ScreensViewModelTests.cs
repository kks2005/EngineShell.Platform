using Engine.Contracts;
using EngineShell.Application.Interfaces;
using Moq;
using Presentation.ViewModels;

namespace Presentation.Tests;

[TestClass]
public sealed class ScreensViewModelTests
{
    [TestMethod]
    public async Task LoadScreensCommand_ProcessesExpectedRequestAndUpdatesStatus()
    {
        // Arrange
        ProcessingRequest? capturedRequest = null;
        var status = new Mock<IAppStatusService>();
        var service = new Mock<IProcessingService>();
        service
            .Setup(x => x.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                null,
                CancellationToken.None))
            .Callback<ProcessingRequest, IProgress<ProcessingProgress>?, CancellationToken>(
                (request, _, _) => capturedRequest = request)
            .ReturnsAsync(new ProcessingResult(true, "output"));
        var sut = new ScreensViewModel(status.Object, service.Object);

        // Act
        await sut.LoadScreensCommand.ExecuteAsync(null);

        // Assert
        Assert.AreEqual("path/to/screens/input", capturedRequest?.InputPath);
        Assert.AreEqual("path/to/screens/output", capturedRequest?.OutputPath);
        status.VerifySet(x => x.Status = "Loading screens...", Times.Once);
        status.VerifySet(x => x.Status = "Loaded True screens", Times.Once);
    }
}
