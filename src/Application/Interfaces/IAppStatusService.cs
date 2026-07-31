using System.ComponentModel;

namespace EngineShell.Application.Interfaces;

public interface IAppStatusService : INotifyPropertyChanged
{
    string Status { get; set; }
    double Progress { get; set; }
}
