using EngineShell.Application.Interfaces;
using System.ComponentModel;

namespace Presentation.ViewModels;

public sealed class FooterViewModel : ViewModelBase
{
    private readonly IAppStatusService _status;

    public string Status => _status.Status;
    public double Progress => _status.Progress;

    public FooterViewModel(IAppStatusService status)
    {
        _status = status;
        _status.PropertyChanged += OnStatusChanged;
    }

    private void OnStatusChanged(
        object? sender,
        PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(IAppStatusService.Status))
        {
            OnPropertyChanged(nameof(Status));
        }
        else if (args.PropertyName == nameof(IAppStatusService.Progress))
        {
            OnPropertyChanged(nameof(Progress));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _status.PropertyChanged -= OnStatusChanged;
        }

        base.Dispose(disposing);
    }
}
