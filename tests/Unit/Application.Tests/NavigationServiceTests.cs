using EngineShell.Application.Models;
using EngineShell.Application.Services;

namespace Application.Tests;

[TestClass]
public sealed class NavigationServiceTests
{
    [TestMethod]
    public void Constructor_SetsFirstViewModelAsCurrent()
    {
        // Arrange
        var firstViewModel = new object();
        var secondViewModel = new object();
        var items = new[]
        {
            new NavigationItem("General", "home", firstViewModel),
            new NavigationItem("Screens", "screens", secondViewModel)
        };

        // Act
        var sut = new NavigationService(items);

        // Assert
        Assert.AreSame(firstViewModel, sut.CurrentViewModel);
    }

    [TestMethod]
    public void NavigateTo_WithKnownKey_UpdatesCurrentViewModel()
    {
        // Arrange
        var firstViewModel = new object();
        var secondViewModel = new object();
        var sut = new NavigationService(
        [
            new NavigationItem("General", "home", firstViewModel),
            new NavigationItem("Screens", "screens", secondViewModel)
        ]);

        // Act
        sut.NavigateTo("Screens");

        // Assert
        Assert.AreSame(secondViewModel, sut.CurrentViewModel);
    }

    [TestMethod]
    public void NavigateTo_WithUnknownKey_KeepsCurrentViewModel()
    {
        // Arrange
        var currentViewModel = new object();
        var sut = new NavigationService(
        [
            new NavigationItem("General", "home", currentViewModel)
        ]);

        // Act
        sut.NavigateTo("Unknown");

        // Assert
        Assert.AreSame(currentViewModel, sut.CurrentViewModel);
    }
}
