using FlaUI.Core.AutomationElements;

namespace UiTest.Infrastructure.PageObjects.Components;

public sealed class FooterComponent
{
    private readonly Window window;

    public FooterComponent(Window window) => this.window = window;

    public string SelectedTheme =>
        ThemePicker.SelectedItem?.Name
        ?? throw new InvalidOperationException(
            "The theme picker does not have a selected item.");

    public void SelectTheme(string theme)
    {
        var selectedItem = ThemePicker.Select(theme);
        if (selectedItem is null)
        {
            throw new InvalidOperationException($"Theme '{theme}' was not found.");
        }

        UiWait.Until(
            () => SelectedTheme == theme,
            $"Theme selection did not become '{theme}'.");
    }

    private ComboBox ThemePicker =>
        UiWait.ForElement(window, AutomationIds.Footer.ThemeComboBox)
            .AsComboBox();
}
