using EngineShell.Application.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EngineShell.Application.Services;

public sealed class AppStatusService : IAppStatusService
{
    private string _status = "Ready";
    private double _progress;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    private void SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
