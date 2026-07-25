namespace Clients.Maui.Views;

public partial class FooterView : ContentView
{
    public FooterView()
    {
        InitializeComponent();
        ThemePicker.SelectedIndex = 0;
        ApplyThemeFromSelection();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (ThemePicker.SelectedIndex < 0)
        {
            ThemePicker.SelectedIndex = 0;
        }

        ApplyThemeFromSelection();
    }

    private void ApplyThemeFromSelection()
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.UserAppTheme =
            ThemePicker.SelectedIndex == 1 ? AppTheme.Dark : AppTheme.Light;
    }
}
