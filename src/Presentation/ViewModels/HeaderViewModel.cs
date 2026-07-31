using CommunityToolkit.Mvvm.ComponentModel;
using Presentation.Navigation;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class HeaderViewModel : ViewModelBase
{
    private bool _suppressNavigation;

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    [ObservableProperty]
    private NavigationItem? selectedNavigationItem;

    public event Action<string>? NavigationChanged;

    public HeaderViewModel(IEnumerable<NavigationItem> items)
    {
        NavigationItems = new ObservableCollection<NavigationItem>(items);
        SelectedNavigationItem = NavigationItems.FirstOrDefault();
    }

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (!_suppressNavigation && value is not null)
        {
            NavigationChanged?.Invoke(value.Title);
        }
    }

    public void RestoreSelection(object viewModel)
    {
        var item = NavigationItems.FirstOrDefault(
            candidate => ReferenceEquals(candidate.ViewModel, viewModel));

        if (item is null)
        {
            return;
        }

        _suppressNavigation = true;
        SelectedNavigationItem = item;
        _suppressNavigation = false;
    }
}

