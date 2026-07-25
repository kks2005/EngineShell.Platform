using FlaUI.Core.AutomationElements;
using Wpf.UiTests.Infrastructure;
using Wpf.UiTests.PageObjects.Pages;

namespace Wpf.UiTests.PageObjects.Components;

public sealed class HeaderComponent
{
    private readonly Window _window;

    internal HeaderComponent(Window window)
    {
        _window = window;
        UiWait.ForElement(_window, AutomationIds.Header.Navigation);
    }

    public GeneralPage SelectGeneral()
    {
        Click(AutomationIds.Header.GeneralItem);
        return new GeneralPage(_window);
    }

    public ScreensPage SelectScreens()
    {
        Click(AutomationIds.Header.ScreensItem);
        return new ScreensPage(_window);
    }

    public RenderEnginePage SelectRenderEngine()
    {
        Click(AutomationIds.Header.RenderEngineItem);
        return new RenderEnginePage(_window);
    }

    private void Click(string automationId)
    {
        UiWait.ForElement(_window, automationId).Click();
    }
}
