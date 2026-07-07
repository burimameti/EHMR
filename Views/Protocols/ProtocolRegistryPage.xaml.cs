using EHMR.ViewModels;

namespace EHMR.Views.Protocols
{
    public partial class ProtocolRegistryPage : ContentPage
    {
        private readonly ProtocolRegistryViewModel _viewModel;
        private readonly MenuView menuView;

        public ProtocolRegistryPage(ProtocolRegistryViewModel viewModel, MenuView menuView)
        {
            InitializeComponent();
            _viewModel=viewModel;
            this.menuView=menuView;
            BindingContext=_viewModel;
            MenuHost.Content=this.menuView;
        }

        protected override async void OnAppearing()
        {
            // Автоматски иницијализирај ја формата со вредностите од ISelectedItemService
            if(BindingContext is ProtocolRegistryViewModel vm)
            {
                await _viewModel.LoadAsync();
            }
        }
    }
}