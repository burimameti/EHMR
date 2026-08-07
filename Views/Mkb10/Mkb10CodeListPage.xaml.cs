using EHMR.ViewModels.Mkb10;
using EHMR.ViewModels.Patients;
using Microsoft.Maui.Controls;

namespace EHMR.Views.Mkb10
{
    public partial class Mkb10CodeListPage : ContentPage
    {
        private readonly Mkb10CodeListViewModel _viewModel;

        public Mkb10CodeListPage(Mkb10CodeListViewModel viewModel, MenuView menu)
        {
            InitializeComponent();

            _viewModel=viewModel;
            BindingContext=_viewModel;

            MenuHost.Content=menu;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if(BindingContext is PatientListViewModel vm)
                await vm.LoadAsync();
        }

    } }
