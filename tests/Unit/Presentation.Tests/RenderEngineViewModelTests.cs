using Engine.Contracts;
using EngineShell.Application.Interfaces;
using Moq;
using Presentation.ViewModels;

namespace Presentation.Tests;

[TestClass]
public sealed class RenderEngineViewModelTests
{
    [TestMethod]
    public void LoadCommand_SetsSampleInputPath()
    {
        // Arrange
        using var eventBus = new EngineEventBus();
        using var sut = new RenderEngineViewModel(Mock.Of<IProcessingService>(), eventBus);

        // Act
        sut.LoadCommand.Execute(null);

        // Assert
        Assert.AreEqual(@"C:\Samples\Input.dat", sut.InputPath);
    }

    [TestMethod]
    public async Task ProcessCommand_WithInput_CompletesAndPassesRequest()
    {
        // Arrange
        ProcessingRequest? capturedRequest = null;
        var service = new Mock<IProcessingService>();
        service
            .Setup(x => x.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProcessingRequest, IProgress<ProcessingProgress>?, CancellationToken>(
                (request, _, _) => capturedRequest = request)
            .ReturnsAsync(new ProcessingResult(true, "output.dat"));
        using var eventBus = new EngineEventBus();
        using var sut = new RenderEngineViewModel(service.Object, eventBus)
        {
            InputPath = "input.dat"
        };

        // Act
        await sut.ProcessCommand.ExecuteAsync(null);

        // Assert
        Assert.AreEqual("input.dat", capturedRequest?.InputPath);
        Assert.AreEqual("Completed", sut.Status);
    }

    [TestMethod]
    public async Task ProcessCommand_WithoutInput_DoesNotCallService()
    {
        // Arrange
        var service = new Mock<IProcessingService>();
        using var eventBus = new EngineEventBus();
        using var sut = new RenderEngineViewModel(service.Object, eventBus);

        // Act
        await sut.ProcessCommand.ExecuteAsync(null);

        // Assert
        service.Verify(
            x => x.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.AreEqual("Ready", sut.Status);
    }

    [TestMethod]
    public async Task ProcessCommand_WhenServiceCancels_SetsCancelledStatus()
    {
        // Arrange
        var service = new Mock<IProcessingService>();
        service
            .Setup(x => x.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        using var eventBus = new EngineEventBus();
        using var sut = new RenderEngineViewModel(service.Object, eventBus)
        {
            InputPath = "input.dat"
        };

        // Act
        await sut.ProcessCommand.ExecuteAsync(null);

        // Assert
        Assert.AreEqual("Cancelled", sut.Status);
    }

    [TestMethod]
    public void EngineEvent_UpdatesStatusUntilViewModelIsDisposed()
    {
        // Arrange
        using var eventBus = new EngineEventBus();
        var sut = new RenderEngineViewModel(Mock.Of<IProcessingService>(), eventBus);

        // Act
        eventBus.Publish(new EngineEvent(
            EngineEventType.Info,
            "Working",
            DateTimeOffset.UtcNow));
        sut.Dispose();
        eventBus.Publish(new EngineEvent(
            EngineEventType.Completed,
            "Should not be observed",
            DateTimeOffset.UtcNow));

        // Assert
        Assert.AreEqual("Working", sut.Status);
    }

    [TestMethod]
    public void ProgressChange_UpdatesProgressTextAndRaisesPropertyChanged()
    {
        // Arrange
        using var eventBus = new EngineEventBus();
        using var sut = new RenderEngineViewModel(Mock.Of<IProcessingService>(), eventBus);
        var changedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        // Act
        sut.Progress = 42;

        // Assert
        Assert.AreEqual("42%", sut.ProgressText);
        CollectionAssert.Contains(changedProperties, nameof(RenderEngineViewModel.Progress));
    }
}
