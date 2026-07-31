namespace Presentation.Navigation;

public sealed class NavigationItem
{
    public string Title { get; }
    public string Icon { get; }
    public object ViewModel { get; }

    public NavigationItem(string title, string icon, object viewModel)
    {
        Title = title;
        Icon = icon;
        ViewModel = viewModel;
    }
}
