using Moq;
using Presentation.Dialogs;
using Presentation.Navigation;

namespace Presentation.Tests;

[TestClass]
public sealed class NavigationServiceTests
{
    [TestMethod]
    public void Constructor_SetsFirstViewModelAsCurrent()
    {
        var firstViewModel = new object();
        var secondViewModel = new object();
        var sut = new NavigationService(
        [
            new NavigationItem("General", "home", firstViewModel),
            new NavigationItem("Screens", "screens", secondViewModel)
        ]);

        Assert.AreSame(firstViewModel, sut.CurrentViewModel);
    }

    [TestMethod]
    public async Task NavigateTo_WithKnownKey_UpdatesCurrentViewModel()
    {
        var firstViewModel = new object();
        var secondViewModel = new object();
        var sut = new NavigationService(
        [
            new NavigationItem("General", "home", firstViewModel),
            new NavigationItem("Screens", "screens", secondViewModel)
        ]);

        var navigated = await sut.NavigateToAsync("Screens");

        Assert.IsTrue(navigated);
        Assert.AreSame(secondViewModel, sut.CurrentViewModel);
    }

    [TestMethod]
    public async Task NavigateTo_WithUnknownKey_KeepsCurrentViewModel()
    {
        var currentViewModel = new object();
        var sut = new NavigationService(
        [
            new NavigationItem("General", "home", currentViewModel)
        ]);

        var navigated = await sut.NavigateToAsync("Unknown");

        Assert.IsFalse(navigated);
        Assert.AreSame(currentViewModel, sut.CurrentViewModel);
    }

    [TestMethod]
    public async Task NavigateTo_WhenUserDeclines_StaysOnCurrentPage()
    {
        var current = new Mock<INavigationAware>();
        current.Setup(x => x.GetNavigationWarning())
            .Returns("Cancel active work and leave?");
        var dialog = new Mock<IDialogService>();
        dialog.Setup(x => x.ShowConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        var destination = new object();
        var sut = new NavigationService(
        [
            new NavigationItem("Current", "current", current.Object),
            new NavigationItem("Next", "next", destination)
        ],
        dialog.Object);

        var navigated = await sut.NavigateToAsync("Next");

        Assert.IsFalse(navigated);
        Assert.AreSame(current.Object, sut.CurrentViewModel);
        current.Verify(
            x => x.OnNavigatedFromAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task NavigateTo_WhenUserConfirms_DeactivatesAndActivatesPages()
    {
        var current = new Mock<INavigationAware>();
        current.Setup(x => x.GetNavigationWarning())
            .Returns("Cancel active work and leave?");
        current.Setup(
                x => x.OnNavigatedFromAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var destination = new Mock<INavigationAware>();
        destination.Setup(
                x => x.OnNavigatedToAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dialog = new Mock<IDialogService>();
        dialog.Setup(x => x.ShowConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        var sut = new NavigationService(
        [
            new NavigationItem("Current", "current", current.Object),
            new NavigationItem("Next", "next", destination.Object)
        ],
        dialog.Object);

        var navigated = await sut.NavigateToAsync("Next");

        Assert.IsTrue(navigated);
        Assert.AreSame(destination.Object, sut.CurrentViewModel);
        current.Verify(
            x => x.OnNavigatedFromAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        destination.Verify(
            x => x.OnNavigatedToAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
