using EngineShell.Application.Interfaces;
using Presentation.Navigation;
using Moq;
using Presentation.ViewModels;

namespace Presentation.Tests;

[TestClass]
public sealed class ShellViewModelTests
{
    [TestMethod]
    public void Constructor_UsesCurrentViewModelFromNavigationService()
    {
        // Arrange
        var current = new object();
        var navigation = new Mock<INavigationService>();
        navigation.SetupGet(x => x.CurrentViewModel).Returns(current);
        var status = Mock.Of<IAppStatusService>();

        // Act
        var sut = new ShellViewModel(
            new HeaderViewModel([]),
            new FooterViewModel(status),
            navigation.Object,
            status);

        // Assert
        Assert.AreSame(current, sut.CurrentViewModel);
    }

    [TestMethod]
    public void HeaderSelection_NavigatesAndUpdatesCurrentViewAndStatus()
    {
        // Arrange
        var initial = new object();
        var destination = new object();
        var item = new NavigationItem("Screens", "screens", destination);
        var header = new HeaderViewModel([]);
        var status = new Mock<IAppStatusService>();
        var navigation = new Mock<INavigationService>();
        navigation.SetupSequence(x => x.CurrentViewModel)
            .Returns(initial)
            .Returns(destination);
        navigation.Setup(
                x => x.NavigateToAsync(
                    "Screens",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = new ShellViewModel(
            header,
            new FooterViewModel(status.Object),
            navigation.Object,
            status.Object);

        // Act
        header.SelectedNavigationItem = item;

        // Assert
        navigation.Verify(
            x => x.NavigateToAsync(
                "Screens",
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.AreSame(destination, sut.CurrentViewModel);
        status.VerifySet(x => x.Status = "Loaded Screens", Times.Once);
        status.VerifySet(x => x.Progress = 0, Times.Once);
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromHeaderNavigation()
    {
        var initial = new object();
        var item = new NavigationItem("Screens", "screens", new object());
        var header = new HeaderViewModel([]);
        var navigation = new Mock<INavigationService>();
        var status = Mock.Of<IAppStatusService>();
        navigation.SetupGet(x => x.CurrentViewModel).Returns(initial);
        navigation.Setup(
                x => x.NavigateToAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = new ShellViewModel(
            header,
            new FooterViewModel(status),
            navigation.Object,
            status);

        sut.Dispose();
        header.SelectedNavigationItem = item;

        navigation.Verify(
            service => service.NavigateToAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public void DeclinedNavigation_RestoresPreviousHeaderSelection()
    {
        var generalViewModel = new object();
        var screensViewModel = new object();
        var general = new NavigationItem(
            "General",
            "general",
            generalViewModel);
        var screens = new NavigationItem(
            "Screens",
            "screens",
            screensViewModel);
        var header = new HeaderViewModel([general, screens]);
        var navigation = new Mock<INavigationService>();
        var status = Mock.Of<IAppStatusService>();
        navigation.SetupGet(x => x.CurrentViewModel)
            .Returns(generalViewModel);
        navigation.Setup(
                x => x.NavigateToAsync(
                    "Screens",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = new ShellViewModel(
            header,
            new FooterViewModel(status),
            navigation.Object,
            status);

        header.SelectedNavigationItem = screens;

        Assert.AreSame(general, header.SelectedNavigationItem);
        Assert.AreSame(generalViewModel, sut.CurrentViewModel);
    }
}
