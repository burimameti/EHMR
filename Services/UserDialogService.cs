using CommunityToolkit.Maui.Extensions;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.ViewModels.Popups;
using EHMR.Views.Popups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    public class UserDialogService : IUserDialogService
    {
        private readonly IEncounterDetailService _encounterService;


        public UserDialogService(IEncounterDetailService encounterService)
        {
            _encounterService=encounterService;
        }


        public Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            var mainPage = GetMainPage();
            return mainPage?.DisplayAlert(title, message, cancel)??Task.CompletedTask;
        }

        public Task<string?> ShowActionSheetAsync(string title, string cancel, string[] options)
        {
            var mainPage = GetMainPage();
            if(mainPage==null)
                return Task.FromResult<string?>(null);
            return mainPage.DisplayActionSheet(title, cancel, null, options);
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            var mainPage = GetMainPage();
            if(mainPage==null)
                return false;
            return await mainPage.DisplayAlert(title, message, accept, cancel);
        }

        public async Task<Appointment?> ShowCreateAppointmentPopupAsync(
            Guid patientId, Guid? doctorId)
        {
            var mainPage = GetMainPage();
            if(mainPage==null)
                return null;

            var viewModel = new CreateAppointmentPopupViewModel(
                _encounterService, patientId, doctorId);

            var popup = new CreateAppointmentPopup(viewModel);

            var result = await mainPage.ShowPopupAsync(popup);

            return result as Appointment;
        }



        public Task<string?> ShowPromptAsync(string title, string message, string accept, string cancel, string placeholder = "", string initialValue = "")
        {
            var mainPage = GetMainPage();
            if(mainPage==null)
                return Task.FromResult<string?>(null);
            return mainPage.DisplayPromptAsync(title, message, accept, cancel, placeholder, -1, Keyboard.Default, initialValue);
        }

        public Task<string?> ShowPromptAsync(string title, string message)
        {
            return ShowPromptAsync(title, message, "OK", "Cancel");
        }

        public async Task ShowMessageAsync(string title, string message)
        {
            var mainPage = GetMainPage();
            if(mainPage==null)
                return;
            await mainPage.DisplayAlert(title, message, "OK");
        }

        /// <summary>
        /// Safely gets the main page of the application.
        /// </summary>
        /// <returns>Current visible page or null if unavailable.</returns>
        private Page? GetMainPage()
        {
            return Application.Current?.Windows.FirstOrDefault()?.Page??Application.Current?.MainPage;
        }
    }
}