using FlaUI.Core.AutomationElements;
using UiTest.Infrastructure.PageObjects.Components;

namespace UiTest.Infrastructure.PageObjects;

public sealed class ShellWindow : PageObject
{
    public ShellWindow(Window window)
        : base(window)
    {
        Header = new HeaderComponent(window);
        Footer = new FooterComponent(window);
    }

    public HeaderComponent Header { get; }
    public FooterComponent Footer { get; }

    public string FooterStatus =>
        Find(AutomationIds.Footer.StatusText).Name;
}
