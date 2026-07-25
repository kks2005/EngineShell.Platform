using UiTest.Infrastructure.PageObjects;
using WinUI.UiTests.Infrastructure;

namespace WinUI.UiTests.Tests;

[TestClass]
public sealed class ThemeTests : UiTestBase
{
    [TestMethod]
    public void SelectingDarkTheme_UpdatesThemeSelection()
    {
        var shell = new ShellWindow(App.MainWindow);
        shell.Footer.SelectTheme("Dark");
        Assert.AreEqual("Dark", shell.Footer.SelectedTheme);
    }
}
