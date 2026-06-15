using EHMR.ViewModels;

namespace EHMR.Views
{
    public partial class UsersPage : ContentPage
    {
        private readonly UsersViewModel _viewModel;
        private readonly MenuView _menu;

        public UsersPage(UsersViewModel viewModel, MenuView menu)
        {
            InitializeComponent();
            _viewModel=viewModel;
            BindingContext=_viewModel;
            _menu=menu;
            MenuHost.Content=_menu;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // ЕДИНСТВЕНО место каде што се повикува вчитување на податоците од базата при влез во страницата
            if(_viewModel!=null)
            {
                await _viewModel.OnAppearingAsync();
            }
        }
    }
}