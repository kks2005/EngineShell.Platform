using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUIClient.Views;

public sealed partial class FooterView : UserControl
{
    private ElementTheme currentTheme = ElementTheme.Light;

    public FooterView()
    {
        InitializeComponent();
        ThemePicker.SelectedIndex = 0;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        ApplyTheme(currentTheme);
    }

    private void ThemePicker_SelectionChanged(
        object sender,
        SelectionChangedEventArgs args)
    {
        if (ThemePicker.SelectedItem is not ComboBoxItem selectedItem ||
            selectedItem.Tag is not string selectedTheme)
        {
            return;
        }

        currentTheme = selectedTheme switch
        {
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Light
        };

        ApplyTheme(currentTheme);
    }

    private void ApplyTheme(ElementTheme theme)
    {
        if (XamlRoot?.Content is FrameworkElement rootContent)
        {
            rootContent.RequestedTheme = theme;
        }
    }
}
