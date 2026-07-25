using CommunityToolkit.Mvvm.ComponentModel;
using EngineShell.Application.Models;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class HeaderViewModel : ViewModelBase
{
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
        if (value is not null)
        {
            NavigationChanged?.Invoke(value.Title);
        }
    }
}

