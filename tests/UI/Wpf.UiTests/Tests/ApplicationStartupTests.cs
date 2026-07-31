using Wpf.UiTests.Infrastructure;
using UiTest.Infrastructure.PageObjects;

namespace Wpf.UiTests.Tests;

[TestClass]
public sealed class ApplicationStartupTests : UiTestBase
{
    [TestMethod]
    public void Launch_ShowsMainWindow()
    {
        // Arrange
        var expectedAutomationId = AutomationIds.Shell.MainWindow;

        // Act
        var shell = new ShellWindow(App.MainWindow);

        // Assert
        Assert.AreEqual(expectedAutomationId, App.MainWindow.AutomationId);
        Assert.IsTrue(App.MainWindow.IsAvailable);
        Assert.IsNotNull(shell.Header);

        var forceFailure = string.Equals(
            Environment.GetEnvironmentVariable("UI_TEST_FORCE_FAILURE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        Assert.IsFalse(
            forceFailure,
            "Intentional failure used to verify UI screenshots and video.");
    }
}
