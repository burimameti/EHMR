namespace EHMR.Views
{
    using Microsoft.Maui.Controls;
    public partial class LoadingPage : ContentPage
    {
        public LoadingPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            this.Opacity=0;

            await this.FadeTo(1, 400);
        }
    }
}