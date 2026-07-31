using Engine.Contracts;
using EngineShell.Application.Exceptions;
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
    public async Task ProcessCommand_WithoutInput_ShowsValidationAndDoesNotCallService()
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
        Assert.AreEqual(
            "Select an input file before processing.",
            sut.Status);
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
    public async Task ProcessCommand_WithExpectedFailure_ShowsReference()
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
                "The file could not be processed.")
            {
                OperationId = "abc12345"
            });
        using var eventBus = new EngineEventBus();
        using var sut = new RenderEngineViewModel(
            service.Object,
            eventBus)
        {
            InputPath = "input.dat"
        };

        await sut.ProcessCommand.ExecuteAsync(null);

        Assert.AreEqual(
            "The file could not be processed. Reference: abc12345",
            sut.Status);
    }

    [TestMethod]
    public async Task ProcessCommand_WithUnexpectedFailure_ShowsReference()
    {
        var service = new Mock<IProcessingService>();
        service
            .Setup(x => x.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProcessingOperationException(
                "def67890",
                "An unexpected processing error occurred.",
                new InvalidOperationException()));
        using var eventBus = new EngineEventBus();
        using var sut = new RenderEngineViewModel(
            service.Object,
            eventBus)
        {
            InputPath = "input.dat"
        };

        await sut.ProcessCommand.ExecuteAsync(null);

        Assert.AreEqual(
            "An unexpected processing error occurred. "
            + "Reference: def67890",
            sut.Status);
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
    public async Task Dispose_CancelsActiveProcessing()
    {
        var started = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new Mock<IProcessingService>();
        service
            .Setup(x => x.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()))
            .Returns<ProcessingRequest, IProgress<ProcessingProgress>?, CancellationToken>(
                async (_, _, token) =>
                {
                    started.TrySetResult(token);
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return new ProcessingResult(true, "output.dat");
                });
        using var eventBus = new EngineEventBus();
        var sut = new RenderEngineViewModel(service.Object, eventBus)
        {
            InputPath = "input.dat"
        };

        var processing = sut.ProcessCommand.ExecuteAsync(null);
        var token = await started.Task;
        sut.Dispose();
        await processing;

        Assert.IsTrue(token.IsCancellationRequested);
    }

    [TestMethod]
    public async Task NavigatingAwayFromActiveProcessing_CancelsAndWaitsForIt()
    {
        var started = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new Mock<IProcessingService>();
        service
            .Setup(x => x.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()))
            .Returns<ProcessingRequest, IProgress<ProcessingProgress>?, CancellationToken>(
                async (_, _, token) =>
                {
                    started.TrySetResult(token);
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return new ProcessingResult(true, "output.dat");
                });
        using var eventBus = new EngineEventBus();
        using var sut = new RenderEngineViewModel(service.Object, eventBus)
        {
            InputPath = "input.dat"
        };

        var processing = sut.ProcessCommand.ExecuteAsync(null);
        var token = await started.Task;

        Assert.AreEqual(
            "Processing is still running. Cancel it and leave this page?",
            sut.GetNavigationWarning());

        await sut.OnNavigatedFromAsync();
        await processing;

        Assert.IsTrue(token.IsCancellationRequested);
        Assert.IsNull(sut.GetNavigationWarning());
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
