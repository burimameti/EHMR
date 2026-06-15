namespace EHMR.Views
{
    public partial class ErrorPage : ContentPage
    {
        private Exception exception;

        public ErrorPage(Exception ex)
        {
            InitializeComponent();
            exception=ex;
        }
    }
}