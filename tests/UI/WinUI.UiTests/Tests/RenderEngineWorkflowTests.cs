using UiTest.Infrastructure.PageObjects;
using WinUI.UiTests.Infrastructure;

namespace WinUI.UiTests.Tests;

[TestClass]
public sealed class RenderEngineWorkflowTests : UiTestBase
{
    [TestMethod]
    public void LoadAndProcess_CompletesWithFullProgress()
    {
        var shell = new ShellWindow(App.MainWindow);
        var renderEngine = shell.Header.SelectRenderEngine();
        renderEngine.Load();
        renderEngine.Process();
        renderEngine.WaitForStatus("Completed");
        renderEngine.WaitForProgress("100%");
        Assert.AreEqual("Completed", renderEngine.Status);
        Assert.AreEqual("100%", renderEngine.ProgressText);
    }
}
