using EHMR.ViewModels;
using Microsoft.Maui.Controls;

namespace EHMR.Views
{
  

    public partial class MedicineDetailFormPage : ContentPage
    {
        private readonly MedicineDetailFormViewModel _viewModel;
        private readonly MenuView menuView;

        public MedicineDetailFormPage(MedicineDetailFormViewModel viewModel, MenuView menuView)
        {
            InitializeComponent();
            _viewModel=viewModel;
            this.menuView=menuView;
            BindingContext=_viewModel;
            MenuHost.Content=this.menuView;
        }
    }
}