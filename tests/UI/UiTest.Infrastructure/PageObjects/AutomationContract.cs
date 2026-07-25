using FlaUI.Core.AutomationElements;

namespace UiTest.Infrastructure.PageObjects;

public static class AutomationContract
{
    public static void VerifySharedTree(Window window)
    {
        var shell = new ShellWindow(window);

        if (!shell.Header.SelectGeneral().IsDisplayed)
        {
            throw new InvalidOperationException(
                "The General view is not available through the shared automation tree.");
        }

        if (!shell.Header.SelectScreens().IsDisplayed)
        {
            throw new InvalidOperationException(
                "The Screens view is not available through the shared automation tree.");
        }

        if (!shell.Header.SelectRenderEngine().IsDisplayed)
        {
            throw new InvalidOperationException(
                "The Render Engine view is not available through the shared automation tree.");
        }

        UiWait.ForElement(window, AutomationIds.Footer.StatusText);
        UiWait.ForElement(window, AutomationIds.Footer.ProgressBar);
    }
}
