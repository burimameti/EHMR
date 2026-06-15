namespace EHMR.Domain.Interfaces
{
    public interface IUserDialogService
    {
        Task<string?> ShowActionSheetAsync(string title, string cancel, string[] options);

        Task<string?> ShowPromptAsync(
    string title,
    string message,
    string accept,
    string cancel,
    string placeholder = "",
    string setialValue = ""
);

        Task ShowAlertAsync(string title, string message, string cancel = "OK");

        Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No");

        Task ShowMessageAsync(string title, string message);
    }
}