using CommunityToolkit.Maui.Views;
using EHMR.ViewModels.Popups;
using System.Runtime.Versioning;
using ZXing;

namespace EHMR.Views.Popups;

public partial class CreateAppointmentPopup : Popup
{
    private readonly CreateAppointmentPopupViewModel _viewModel;

    public CreateAppointmentPopup(CreateAppointmentPopupViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
#if WINDOWS
        BindingContext = _viewModel;
        viewModel.RequestClose = result =>
        {
            AppointmentResult = result;
            ClosePopup();
        };
        _=_viewModel.InitializeAsync();
#else
        viewModel.RequestClose = result =>
        {
            AppointmentResult = result;
            CloseAsync();
        };
#endif
    }

    public object? AppointmentResult { get; private set; }

   

    [SupportedOSPlatform("windows10.0.17763.0")]
    private void ClosePopup()
    {
#if WINDOWS
        _ = CloseAsync(CancellationToken.None);
#else
        _ = CloseAsync();
#endif
    }
}