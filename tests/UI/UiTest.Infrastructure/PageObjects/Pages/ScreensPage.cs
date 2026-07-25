using FlaUI.Core.AutomationElements;

namespace UiTest.Infrastructure.PageObjects.Pages;

public sealed class ScreensPage : PageObject
{
    public ScreensPage(Window window)
        : base(window)
    {
        Find(AutomationIds.Screens.LoadButton);
    }

    public bool IsDisplayed =>
        Find(AutomationIds.Screens.LoadButton).IsAvailable;

    public string BusyText =>
        Find(AutomationIds.Screens.BusyText).Name;

    public void Load()
    {
        Find(AutomationIds.Screens.LoadButton).Click();
    }

    public void Refresh()
    {
        Find(AutomationIds.Screens.RefreshButton).Click();
    }
}
