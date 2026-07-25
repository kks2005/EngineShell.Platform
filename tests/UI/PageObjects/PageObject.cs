using FlaUI.Core.AutomationElements;
using Wpf.UiTests.Infrastructure;

namespace Wpf.UiTests.PageObjects;

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
