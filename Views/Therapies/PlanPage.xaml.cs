using EHMR.ViewModels;

namespace EHMR.Views.Therapies
{
    public partial class PlanPage : ContentPage
    {
        private readonly PlansViewModel _viewModel;
        private readonly MenuView _menu;

        public PlanPage(PlansViewModel viewModel, MenuView menu)
        {
            InitializeComponent();

            _viewModel=viewModel;
            _menu=menu;
            BindingContext=_viewModel;
            MenuHost.Content=menu;
        }

        protected override async void OnAppearing()
        {
            // ЕДИНСТВЕНО место каде што се повикува вчитување на податоците од базата при влез во страницата
            if(_viewModel!=null)
            {
                await _viewModel.LoadPlansAsync();
            }
        }
    }
}