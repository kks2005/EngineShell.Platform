namespace Presentation.Dialogs;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string message);
}
