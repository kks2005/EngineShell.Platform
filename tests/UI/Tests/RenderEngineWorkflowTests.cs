using Wpf.UiTests.Infrastructure;
using Wpf.UiTests.PageObjects;

namespace Wpf.UiTests.Tests;

[TestClass]
public sealed class RenderEngineWorkflowTests : UiTestBase
{
    [TestMethod]
    public void LoadAndProcess_CompletesWithFullProgress()
    {
        // Arrange
        var shell = new ShellWindow(App.MainWindow);
        var renderEngine = shell.Header.SelectRenderEngine();

        // Act
        renderEngine.Load();
        renderEngine.Process();
        renderEngine.WaitForStatus("Completed");
        renderEngine.WaitForProgress("100%");

        // Assert
        Assert.AreEqual("Completed", renderEngine.Status);
        Assert.AreEqual("100%", renderEngine.ProgressText);
    }
}
