using CommunityToolkit.Maui.Views;
using EHMR.ViewModels.Popups;

namespace EHMR.Views.Popups;

public partial class CreateAppointmentPopup : Popup
{
    private readonly CreateAppointmentPopupViewModel _viewModel;

    public CreateAppointmentPopup(CreateAppointmentPopupViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestClose = result =>
        {
            AppointmentResult = result;
            ClosePopup(result);
        };

        _ = _viewModel.InitializeAsync();
    }

    public object? AppointmentResult { get; private set; }

   

    private void ClosePopup(object? result)
    {
#if WINDOWS
        _ = CloseAsync(result, CancellationToken.None);
#else
        _ = CloseAsync(result);
#endif
    }
}