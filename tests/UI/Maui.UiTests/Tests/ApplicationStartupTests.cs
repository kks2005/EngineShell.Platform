using Maui.UiTests.Infrastructure;
using UiTest.Infrastructure.PageObjects;

namespace Maui.UiTests.Tests;

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