using FlaUI.Core.AutomationElements;
using Wpf.UiTests.Infrastructure;

namespace Wpf.UiTests.PageObjects.Pages;

public sealed class RenderEnginePage : PageObject
{
    internal RenderEnginePage(Window window)
        : base(window)
    {
        Find(AutomationIds.RenderEngine.Title);
    }

    public bool IsDisplayed =>
        Find(AutomationIds.RenderEngine.Title).IsAvailable;

    public string Status =>
        Find(AutomationIds.RenderEngine.StatusText).Name;

    public string ProgressText =>
        Find(AutomationIds.RenderEngine.ProgressText).Name;

    public void Load()
    {
        Find(AutomationIds.RenderEngine.LoadButton).Click();
    }

    public void Process()
    {
        Find(AutomationIds.RenderEngine.ProcessButton).Click();
    }

    public void Cancel()
    {
        Find(AutomationIds.RenderEngine.CancelButton).Click();
    }

    public void WaitForStatus(string expectedStatus)
    {
        UiWait.Until(
            () => Status == expectedStatus,
            $"Render Engine status did not become '{expectedStatus}'.");
    }

    public void WaitForProgress(string expectedProgress)
    {
        UiWait.Until(
            () => ProgressText == expectedProgress,
            $"Render Engine progress did not become '{expectedProgress}'.");
    }
}
