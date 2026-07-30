using EngineShell.Application.Interfaces;
using EngineShell.Application.Models;
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

        // Act
        var sut = new ShellViewModel(
            new HeaderViewModel([]),
            new FooterViewModel(),
            navigation.Object,
            Mock.Of<IAppStatusService>());

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
        var sut = new ShellViewModel(
            header,
            new FooterViewModel(),
            navigation.Object,
            status.Object);

        // Act
        header.SelectedNavigationItem = item;

        // Assert
        navigation.Verify(x => x.NavigateTo("Screens"), Times.Once);
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
        navigation.SetupGet(x => x.CurrentViewModel).Returns(initial);
        var sut = new ShellViewModel(
            header,
            new FooterViewModel(),
            navigation.Object,
            Mock.Of<IAppStatusService>());

        sut.Dispose();
        header.SelectedNavigationItem = item;

        navigation.Verify(
            service => service.NavigateTo(It.IsAny<string>()),
            Times.Never);
    }
}
