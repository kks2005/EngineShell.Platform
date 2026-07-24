namespace EngineShell.Application.Interfaces;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string message);
    Task<string?> ShowInputAsync(string prompt);
    Task ShowMessageAsync(string message);
}


