using EHMR.ViewModels;
using EHMR.ViewModels.Mkb10;

namespace EHMR.Views.Mkb10
{
   

    public partial class Mkb10CodeDetailPage : ContentPage
    {
        private readonly Mkb10CodeDetailViewModel _viewModel;
        private readonly MenuView menuView;

        public Mkb10CodeDetailPage(Mkb10CodeDetailViewModel viewModel, MenuView menuView)
        {
            InitializeComponent();
            _viewModel=viewModel;
            this.menuView=menuView;
            BindingContext=_viewModel;
            MenuHost.Content=this.menuView;
        }
    }
}
