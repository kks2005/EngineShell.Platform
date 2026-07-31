using CommunityToolkit.Mvvm.ComponentModel;
using EngineShell.Application.Interfaces;
using Presentation.Navigation;

namespace Presentation.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    public HeaderViewModel Header { get; }
    public FooterViewModel Footer { get; }
    public IAppStatusService Status { get; }
    public INavigationService Navigation { get; }

    [ObservableProperty]
    private object currentViewModel;

    public ShellViewModel(
        HeaderViewModel header,
        FooterViewModel footer,
        INavigationService navigation,
        IAppStatusService status)
    {
        Header = header;
        Footer = footer;
        Navigation = navigation;
        Status = status;

        Header.NavigationChanged += OnNavigationChanged;

        CurrentViewModel = Navigation.CurrentViewModel;
    }

    private async void OnNavigationChanged(string key)
    {
        var previousViewModel = CurrentViewModel;
        var navigated = await Navigation.NavigateToAsync(key);

        if (!navigated)
        {
            Header.RestoreSelection(previousViewModel);
            return;
        }

        CurrentViewModel = Navigation.CurrentViewModel;

        Status.Status = $"Loaded {key}";
        Status.Progress = 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Header.NavigationChanged -= OnNavigationChanged;
        }

        base.Dispose(disposing);
    }
}
