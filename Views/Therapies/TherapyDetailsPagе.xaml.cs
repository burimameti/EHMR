using EHMR.ViewModels;
using EHMR.ViewModels.Therapies;

namespace EHMR.Views.Therapies;

public partial class TherapyDetailsPage : ContentPage
{
    private readonly TherapyDetailsViewModel _viewModel;
    private readonly MenuView _menu;

    public TherapyDetailsPage(TherapyDetailsViewModel viewModel, MenuView menu)
    {
        InitializeComponent();
        BindingContext=viewModel;
        _viewModel=viewModel;
        _menu=menu;
        MenuHost.Content=_menu;
    }
}