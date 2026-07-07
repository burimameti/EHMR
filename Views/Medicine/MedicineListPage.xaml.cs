using EHMR.ViewModels;

namespace EHMR.Views
{
    public partial class MedicineListPage : ContentPage
    {
        private readonly MedicineListViewModel _viewModel;
        private readonly MenuView menuView;

        public MedicineListPage(MedicineListViewModel viewModel, MenuView menuView)
        {
            _viewModel=viewModel;
            this.menuView=menuView;

            InitializeComponent();
            BindingContext=_viewModel;
            MenuHost.Content = this.menuView;
        }

        protected void OnAppearing()
        {
            base.OnAppearing();
            if(BindingContext is MedicineListViewModel vm)
            {
                _=vm.LoadAsync();
            }
        }
    }
}