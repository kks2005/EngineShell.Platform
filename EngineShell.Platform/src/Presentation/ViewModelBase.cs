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

    protected virtual void Dispose(bool disposing)
    {
    }
}