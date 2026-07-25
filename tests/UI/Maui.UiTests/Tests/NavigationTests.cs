using Maui.UiTests.Infrastructure;
using UiTest.Infrastructure.PageObjects;

namespace Maui.UiTests.Tests;

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