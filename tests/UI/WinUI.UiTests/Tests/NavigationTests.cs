using UiTest.Infrastructure.PageObjects;
using WinUI.UiTests.Infrastructure;

namespace WinUI.UiTests.Tests;

[TestClass]
public sealed class NavigationTests : UiTestBase
{
    [TestMethod]
    public void ClickingHeaderItems_DisplaysSharedViews()
    {
        var shell = new ShellWindow(App.MainWindow);
        Assert.IsTrue(shell.Header.SelectGeneral().IsDisplayed);
        Assert.IsTrue(shell.Header.SelectScreens().IsDisplayed);
        Assert.IsTrue(shell.Header.SelectRenderEngine().IsDisplayed);
    }
}
