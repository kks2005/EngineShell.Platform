using Presentation.Dialogs;

namespace Presentation.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly Dictionary<string, object> _pages;
    private readonly IDialogService? _dialogService;

    public object CurrentViewModel { get; private set; }

    public NavigationService(
        IEnumerable<NavigationItem> items,
        IDialogService? dialogService = null)
    {
        _pages = items.ToDictionary(x => x.Title, x => x.ViewModel);
        CurrentViewModel = _pages.Values.First();
        _dialogService = dialogService;
    }

    public async Task<bool> NavigateToAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (!_pages.TryGetValue(key, out var destination))
        {
            return false;
        }

        if (ReferenceEquals(destination, CurrentViewModel))
        {
            return true;
        }

        if (CurrentViewModel is INavigationAware current)
        {
            var warning = current.GetNavigationWarning();

            if (warning is not null)
            {
                var confirmed = _dialogService is not null
                    && await _dialogService.ShowConfirmationAsync(warning);

                if (!confirmed)
                {
                    return false;
                }
            }

            await current.OnNavigatedFromAsync(cancellationToken);
        }

        CurrentViewModel = destination;

        if (destination is INavigationAware next)
        {
            await next.OnNavigatedToAsync(cancellationToken);
        }

        return true;
    }
}
