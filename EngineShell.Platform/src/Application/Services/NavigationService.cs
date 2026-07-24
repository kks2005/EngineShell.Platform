using EngineShell.Application.Interfaces;
using EngineShell.Application.Models;

namespace EngineShell.Application.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Dictionary<string, object> _pages;

        public object CurrentViewModel { get; private set; }

        public NavigationService(IEnumerable<NavigationItem> items)
        {
            _pages = items.ToDictionary(x => x.Title, x => x.ViewModel);
            CurrentViewModel = _pages.Values.First();
        }

        public void NavigateTo(string key)
        {
            if (_pages.TryGetValue(key, out var vm))
                CurrentViewModel = vm;
        }
    }
}
