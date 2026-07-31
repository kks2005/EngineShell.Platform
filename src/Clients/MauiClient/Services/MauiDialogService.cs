using Presentation.Dialogs;

namespace Clients.Maui.Services;

public sealed class MauiDialogService : IDialogService
{
    public Task<bool> ShowConfirmationAsync(string message)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;

        return page is null
            ? Task.FromResult(false)
            : page.DisplayAlert(
                "Confirm navigation",
                message,
                "Yes",
                "No");
    }
}
