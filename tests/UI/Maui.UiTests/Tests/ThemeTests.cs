using Maui.UiTests.Infrastructure;
using UiTest.Infrastructure.PageObjects;

namespace Maui.UiTests.Tests;

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