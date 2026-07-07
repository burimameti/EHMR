using EHMR.ViewModels;

namespace EHMR.Views.Therapies
{
    public partial class ProtocolDetailFormPage : ContentPage
    {
        private readonly ProtocolDetailFormViewModel _viewModel;
        private readonly MenuView _menu;

        public ProtocolDetailFormPage(MenuView menu, ProtocolDetailFormViewModel viewModel)
        {
            InitializeComponent();
            _menu=menu;
            _viewModel=viewModel;
            BindingContext=_viewModel;
            MenuHost.Content=_menu;
        }
    }
}