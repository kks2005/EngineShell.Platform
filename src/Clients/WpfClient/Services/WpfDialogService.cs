using EngineShell.Application.Interfaces;
using System.Windows;

namespace WpfClient.Services;

public class WpfDialogService : IDialogService
{
    public Task<bool> ShowConfirmationAsync(string message)
    {
        throw new NotImplementedException();
    }

    public Task<string?> ShowInputAsync(string prompt)
    {
        throw new NotImplementedException();
    }

    public Task ShowMessageAsync(string message)
    {
        throw new NotImplementedException();
    }
}

