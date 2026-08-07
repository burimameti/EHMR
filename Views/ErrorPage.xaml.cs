namespace EHMR.Views
{
    using Microsoft.Maui.Controls;
    using Microsoft.Maui.ApplicationModel.DataTransfer;
    using System;

    public partial class ErrorPage : ContentPage
    {
        private readonly Exception _exception;

        public ErrorPage(Exception ex)
        {
            InitializeComponent();
            _exception=ex;

            MessageLabel.Text=ex.Message;

            InnerMessageLabel.Text=ex.InnerException!=null
                ? $"Внатрешна грешка: {ex.InnerException.Message}"
                : string.Empty;

            LogPathLabel.Text=$"Целосен лог: {Logger.GetLogFilePath()}";

            StackTraceLabel.Text=ex.StackTrace??"(нема stack trace)";
        }

        private async void OnCopyClicked(object sender, EventArgs e)
        {
            var fullText = $"{_exception}\n\nLog file: {Logger.GetLogFilePath()}";
            await Clipboard.SetTextAsync(fullText);
            await DisplayAlert("Копирано", "Деталите се копирани во clipboard.", "OK");
        }
    }
}