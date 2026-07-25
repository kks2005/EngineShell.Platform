using UiTest.Infrastructure.PageObjects;
using WinUI.UiTests.Infrastructure;

namespace WinUI.UiTests.Tests;

[TestClass]
public sealed class ApplicationStartupTests : UiTestBase
{
    [TestMethod]
    public void Launch_ShowsSharedShell()
    {
        var shell = new ShellWindow(App.MainWindow);
        Assert.IsTrue(App.MainWindow.IsAvailable);
        Assert.IsNotNull(shell.Header);
    }
}
