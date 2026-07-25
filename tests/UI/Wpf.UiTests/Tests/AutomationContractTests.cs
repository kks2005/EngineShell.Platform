using UiTest.Infrastructure.PageObjects;
using Wpf.UiTests.Infrastructure;

namespace Wpf.UiTests.Tests;

[TestClass]
public sealed class AutomationContractTests : UiTestBase
{
    [TestMethod]
    public void SharedAutomationTree_IsComplete()
    {
        AutomationContract.VerifySharedTree(App.MainWindow);
    }
}
