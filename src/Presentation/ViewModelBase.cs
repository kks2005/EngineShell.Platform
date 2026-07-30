using CommunityToolkit.Mvvm.ComponentModel;

namespace Presentation;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private bool disposed;

    protected bool IsDisposed => disposed;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    /// <summary>
    /// Releases resources owned by the view model.
    /// Override this method when a derived view model owns event subscriptions,
    /// timers, cancellation sources, or other disposable resources. Release
    /// managed resources only when <paramref name="disposing"/> is <see langword="true"/>,
    /// and call the base implementation after completing derived cleanup.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
    }
}
