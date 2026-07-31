using Presentation.Dialogs;
using System.Windows;

namespace WpfClient.Services;

public sealed class WpfDialogService : IDialogService
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
}
