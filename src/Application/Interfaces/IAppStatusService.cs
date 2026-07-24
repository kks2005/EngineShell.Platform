namespace EngineShell.Application.Interfaces;

public interface IAppStatusService
{
    string Status { get; set; }
    double Progress { get; set; }
    void Log(string message);
}

