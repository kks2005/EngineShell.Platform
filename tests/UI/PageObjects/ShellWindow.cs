using FlaUI.Core.AutomationElements;
using Wpf.UiTests.PageObjects.Components;

namespace Wpf.UiTests.PageObjects;

public sealed class ShellWindow : PageObject
{
    public ShellWindow(Window window)
        : base(window)
    {
        if (window.AutomationId != AutomationIds.Shell.MainWindow)
        {
            throw new InvalidOperationException(
                $"Expected window '{AutomationIds.Shell.MainWindow}', "
                + $"but found '{window.AutomationId}'.");
        }

        Header = new HeaderComponent(window);
    }

    public HeaderComponent Header { get; }

    public string FooterStatus =>
        Find(AutomationIds.Footer.StatusText).Name;
}
