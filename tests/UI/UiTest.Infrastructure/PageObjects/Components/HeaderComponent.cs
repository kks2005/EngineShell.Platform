using FlaUI.Core.AutomationElements;
using UiTest.Infrastructure.PageObjects.Pages;

namespace UiTest.Infrastructure.PageObjects.Components;

public sealed class HeaderComponent
{
    private readonly Window _window;
    private readonly AutomationElement _navigation;

    public HeaderComponent(Window window)
    {
        _window = window;
        _navigation = UiWait.ForElement(_window, AutomationIds.Header.Navigation);
    }

    public GeneralPage SelectGeneral()
    {
        Select(AutomationIds.Header.GeneralItem);
        return new GeneralPage(_window);
    }

    public ScreensPage SelectScreens()
    {
        Select(AutomationIds.Header.ScreensItem);
        return new ScreensPage(_window);
    }

    public RenderEnginePage SelectRenderEngine()
    {
        Select(AutomationIds.Header.RenderEngineItem);
        return new RenderEnginePage(_window);
    }

    private void Select(string automationId)
    {
        var element = UiWait.ForElementByAutomationIdOrName(
            _navigation,
            automationId);

        for (var candidate = element;
             candidate is not null;
             candidate = candidate.Parent)
        {
            if (candidate.Patterns.SelectionItem.IsSupported)
            {
                candidate.Patterns.SelectionItem.Pattern.Select();
                return;
            }

            if (candidate.Equals(_navigation))
            {
                break;
            }
        }

        throw new InvalidOperationException(
            $"Navigation item '{automationId}' does not expose "
            + "the UIA SelectionItem pattern.");
    }
}
