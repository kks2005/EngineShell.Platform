using EngineShell.Application.Interfaces;
using System.Windows;

namespace WpfClient.Services;

public class WpfDialogService : IDialogService
{
    public Task<bool> ShowConfirmationAsync(string message)
    {
        var result = MessageBox.Show(
            message,
            "Confirm navigation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return Task.FromResult(result == MessageBoxResult.Yes);
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

