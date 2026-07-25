using FlaUI.Core.AutomationElements;

namespace Wpf.UiTests.PageObjects.Pages;

public sealed class GeneralPage : PageObject
{
    internal GeneralPage(Window window)
        : base(window)
    {
        Find(AutomationIds.General.Title);
    }

    public bool IsDisplayed =>
        Find(AutomationIds.General.Title).IsAvailable;

    public void EnterUserName(string userName)
    {
        Find(AutomationIds.General.UserNameTextBox).AsTextBox().Text = userName;
    }

    public void Save()
    {
        Find(AutomationIds.General.SaveButton).Click();
    }
}
