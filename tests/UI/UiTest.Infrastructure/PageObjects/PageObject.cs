using FlaUI.Core.AutomationElements;
using UiTest.Infrastructure;

namespace UiTest.Infrastructure.PageObjects;

public abstract class PageObject
{
    protected PageObject(Window window)
    {
        Window = window;
    }

    protected Window Window { get; }

    protected AutomationElement Find(string automationId)
    {
        return UiWait.ForElement(Window, automationId);
    }
}
