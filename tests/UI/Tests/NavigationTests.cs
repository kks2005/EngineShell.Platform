using Wpf.UiTests.Infrastructure;
using Wpf.UiTests.PageObjects;

namespace Wpf.UiTests.Tests;

[TestClass]
public sealed class NavigationTests : UiTestBase
{
    [TestMethod]
    public void ClickingHeaderItems_DisplaysEachExpectedCurrentView()
    {
        // Arrange
        var shell = new ShellWindow(App.MainWindow);

        // Act and Assert
        var general = shell.Header.SelectGeneral();
        Assert.IsTrue(general.IsDisplayed);

        var screens = shell.Header.SelectScreens();
        Assert.IsTrue(screens.IsDisplayed);

        var renderEngine = shell.Header.SelectRenderEngine();
        Assert.IsTrue(renderEngine.IsDisplayed);

        var generalAgain = shell.Header.SelectGeneral();
        Assert.IsTrue(generalAgain.IsDisplayed);
    }
}
