using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Presentation.Dialogs;

namespace WinUIClient.Services;

public sealed class WinUiDialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string message)
    {
        var app = (App)Application.Current;
        var root = app.MainWindow?.Content as FrameworkElement;

        if (root?.XamlRoot is null)
        {
            return false;
        }

        var dialog = new ContentDialog
        {
            Title = "Confirm navigation",
            Content = message,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = root.XamlRoot
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
