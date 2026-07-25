using FlaUI.Core.AutomationElements;

namespace Wpf.UiTests.PageObjects.Pages;

public sealed class ScreensPage : PageObject
{
    internal ScreensPage(Window window)
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
