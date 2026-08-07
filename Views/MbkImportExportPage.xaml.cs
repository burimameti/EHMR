using EHMR.ViewModels.Support;
using Microsoft.Maui.Controls;
using System;

namespace EHMR.Views
{
    public partial class MbkImportExportPage : ContentPage
    {
        public MbkImportExportPage(MbkImportExportViewModel vm)
        {
            InitializeComponent();
            BindingContext=vm;
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            if(Navigation.ModalStack.Count>0)
            {
                await Navigation.PopModalAsync();
            }
            else
            {
                await Navigation.PopAsync();
            }
        }
    }
}