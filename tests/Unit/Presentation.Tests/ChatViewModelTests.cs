using EngineShell.Application.AI;
using EngineShell.Application.Exceptions;
using EngineShell.Application.Interfaces;
using Moq;
using Presentation.ViewModels;

namespace Presentation.Tests;

[TestClass]
public sealed class ChatViewModelTests
{
    [TestMethod]
    public void SelectSuggestionCommand_PopulatesEditableInput()
    {
        var sut = new ChatViewModel(Mock.Of<IChatService>());
        var suggestion = sut.Suggestions[1];

        sut.SelectSuggestionCommand.Execute(suggestion);

        Assert.AreEqual(suggestion.Prompt, sut.Input);
        Assert.IsTrue(sut.SendCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task SendCommand_AddsUserAndAssistantMessages()
    {
        var chatService = new Mock<IChatService>();
        chatService
            .Setup(service => service.SendAsync(
                "process input.dat",
                It.IsAny<IProgress<ChatProgress>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResult("Processing completed."));
        var sut = new ChatViewModel(chatService.Object)
        {
            Input = "process input.dat"
        };

        await sut.SendCommand.ExecuteAsync(null);

        Assert.AreEqual(string.Empty, sut.Input);
        Assert.AreEqual("You", sut.Messages[^2].Speaker);
        Assert.AreEqual("process input.dat", sut.Messages[^2].Text);
        Assert.AreEqual("Assistant", sut.Messages[^1].Speaker);
        Assert.AreEqual("Processing completed.", sut.Messages[^1].Text);
    }

    [TestMethod]
    public async Task SendCommand_WithProcessingFailure_ShowsReference()
    {
        var chatService = new Mock<IChatService>();
        chatService
            .Setup(service => service.SendAsync(
                "process input.dat",
                It.IsAny<IProgress<ChatProgress>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProcessingOperationException(
                "abc12345",
                "An unexpected processing error occurred.",
                new InvalidOperationException()));
        var sut = new ChatViewModel(chatService.Object)
        {
            Input = "process input.dat"
        };

        await sut.SendCommand.ExecuteAsync(null);

        Assert.AreEqual("System", sut.Messages[^1].Speaker);
        Assert.AreEqual(
            "An unexpected processing error occurred. "
            + "Reference: abc12345",
            sut.Messages[^1].Text);
    }
}
