using EHMR.ViewModels;

namespace EHMR.Views
{
    public partial class PatientDetailFormPage : ContentPage
    {
        private readonly PatientDetailFormViewModel _viewModel;
        private readonly MenuView menuView;

        public PatientDetailFormPage(PatientDetailFormViewModel viewModel, MenuView menuView)
        {
            InitializeComponent();
            _viewModel=viewModel;
            this.menuView=menuView;
            BindingContext=_viewModel;
            MenuHost.Content=this.menuView;
        }
    }
}