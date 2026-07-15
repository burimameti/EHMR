using CommunityToolkit.Maui.Views;
using EHMR.ViewModels.Popups;
namespace EHMR.Views.Popups;
public partial class CreateTherapyCyclePopup : Popup
{
    private readonly CreateTherapyCyclePopupViewModel _viewModel;
    public CreateTherapyCyclePopup(CreateTherapyCyclePopupViewModel viewModel)
    {
        InitializeComponent();
        _viewModel=viewModel;
        BindingContext=_viewModel;
        viewModel.RequestClose=result =>
        {
            TherapyCycleResult=result;
            _=CloseAsync(CancellationToken.None);
        };
    }

    public object? TherapyCycleResult
    {
        get; private set;
    }
}