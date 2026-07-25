using FlaUI.Core.AutomationElements;

namespace UiTest.Infrastructure.PageObjects.Pages;

public sealed class RenderEnginePage : PageObject
{
    public RenderEnginePage(Window window)
        : base(window)
    {
        Find(AutomationIds.RenderEngine.Title);
    }

    public bool IsDisplayed =>
        Find(AutomationIds.RenderEngine.Title).IsAvailable;

    public string Status =>
        Find(AutomationIds.RenderEngine.StatusText).Name;

    public string InputPath =>
        Find(AutomationIds.RenderEngine.InputPathText).Name;

    public string ProgressText =>
        Find(AutomationIds.RenderEngine.ProgressText).Name;

    public void Load()
    {
        Find(AutomationIds.RenderEngine.LoadButton).AsButton().Invoke();
        UiWait.Until(
            () => !string.IsNullOrWhiteSpace(InputPath),
            "Render Engine input path was not loaded.");
    }

    public void Process()
    {
        Find(AutomationIds.RenderEngine.ProcessButton).AsButton().Invoke();
    }

    public void Cancel()
    {
        Find(AutomationIds.RenderEngine.CancelButton).AsButton().Invoke();
    }

    public void WaitForStatus(string expectedStatus)
    {
        try
        {
            UiWait.Until(
                () => Status == expectedStatus,
                $"Render Engine status did not become '{expectedStatus}'.");
        }
        catch (TimeoutException exception)
        {
            throw new TimeoutException(
                $"Render Engine status did not become '{expectedStatus}'. "
                + $"Actual status: '{Status}', progress: '{ProgressText}', "
                + $"input: '{InputPath}'.",
                exception);
        }
    }

    public void WaitForProgress(string expectedProgress)
    {
        UiWait.Until(
            () => ProgressText == expectedProgress,
            $"Render Engine progress did not become '{expectedProgress}'.");
    }
}
