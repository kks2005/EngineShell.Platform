namespace EngineShell.Application.Interfaces;

/// <summary>
/// Allows a view model to participate in navigation when it has active work
/// or other state that should be handled before the user leaves the page.
/// </summary>
public interface INavigationAware
{
    /// <summary>
    /// Returns a confirmation message when leaving requires user approval;
    /// otherwise, returns <see langword="null"/>.
    /// </summary>
    string? GetNavigationWarning();

    Task OnNavigatedFromAsync(
        CancellationToken cancellationToken = default);

    Task OnNavigatedToAsync(
        CancellationToken cancellationToken = default);
}
