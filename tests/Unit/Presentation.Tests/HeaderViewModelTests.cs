using EngineShell.Application.Models;
using Presentation.ViewModels;

namespace Presentation.Tests;

[TestClass]
public sealed class HeaderViewModelTests
{
    [TestMethod]
    public void Constructor_PopulatesItemsAndSelectsFirstItem()
    {
        // Arrange
        var first = new NavigationItem("General", "home", new object());
        var second = new NavigationItem("Screens", "screens", new object());

        // Act
        var sut = new HeaderViewModel([first, second]);

        // Assert
        CollectionAssert.AreEqual(new[] { first, second }, sut.NavigationItems);
        Assert.AreSame(first, sut.SelectedNavigationItem);
    }

    [TestMethod]
    public void SelectingItem_RaisesNavigationChangedWithTitle()
    {
        // Arrange
        var first = new NavigationItem("General", "home", new object());
        var second = new NavigationItem("Screens", "screens", new object());
        var sut = new HeaderViewModel([first, second]);
        object? navigationKey = null;
        sut.NavigationChanged += key => navigationKey = key;

        // Act
        sut.SelectedNavigationItem = second;

        // Assert
        Assert.AreEqual("Screens", navigationKey);
    }
}
