namespace Presentation.Navigation;

public interface INavigationService
{
    object CurrentViewModel { get; }

    Task<bool> NavigateToAsync(
        string key,
        CancellationToken cancellationToken = default);
}
