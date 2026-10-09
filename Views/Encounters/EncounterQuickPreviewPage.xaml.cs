using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;

namespace EHMR.Views.Encounters;

public partial class EncounterQuickPreviewPage : ContentPage
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;

    public EncounterQuickPreviewPage(ISelectedItemService<Encounter> selectedItemService)
    {
        InitializeComponent();
        _selectedItemService = selectedItemService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = _selectedItemService.SelectedItem;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}