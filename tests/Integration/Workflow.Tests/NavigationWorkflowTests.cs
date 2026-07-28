using EngineShell.Application.Models;
using EngineShell.Application.Services;
using Presentation.ViewModels;

namespace Integration.Tests;

[TestClass]
public sealed class NavigationWorkflowTests
{
    [TestMethod]
    public void HeaderSelection_UpdatesNavigationShellAndApplicationStatus()
    {
        // Arrange
        var generalViewModel = new GeneralViewModel();
        var screensViewModel = new object();
        var general = new NavigationItem("General", "home", generalViewModel);
        var screens = new NavigationItem("Screens", "screens", screensViewModel);
        var items = new[] { general, screens };
        var header = new HeaderViewModel(items);
        var navigation = new NavigationService(items);
        var status = new AppStatusService { Progress = 75 };
        var sut = new ShellViewModel(
            header,
            new FooterViewModel(),
            navigation,
            status);

        // Act
        header.SelectedNavigationItem = screens;

        // Assert
        Assert.AreSame(screensViewModel, navigation.CurrentViewModel);
        Assert.AreSame(screensViewModel, sut.CurrentViewModel);
        Assert.AreEqual("Loaded Screens", status.Status);
        Assert.AreEqual(0d, status.Progress);
    }
}
