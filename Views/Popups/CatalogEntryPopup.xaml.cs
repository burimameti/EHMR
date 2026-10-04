using CommunityToolkit.Maui.Views;
using EHMR.ViewModels.Popups;

namespace EHMR.Views.Popups;

public partial class CatalogEntryPopup : Popup
{
    public CatalogEntryPopup(CatalogEntryPopupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext=viewModel;
        viewModel.RequestClose=result => _=CloseAsync(result);
    }
}
