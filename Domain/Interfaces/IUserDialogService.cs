using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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


        /// Opens a popup to create a brand-new appointment. Returns null if the user cancels.
        Task<Appointment?> ShowCreateAppointmentPopupAsync(
          Guid patientId, Guid? doctorId);

    }
}