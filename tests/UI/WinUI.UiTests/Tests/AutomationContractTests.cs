using UiTest.Infrastructure.PageObjects;
using WinUI.UiTests.Infrastructure;

namespace WinUI.UiTests.Tests;

[TestClass]
public sealed class AutomationContractTests : UiTestBase
{
    [TestMethod]
    public void SharedAutomationTree_IsComplete()
    {
        AutomationContract.VerifySharedTree(App.MainWindow);
    }
}
