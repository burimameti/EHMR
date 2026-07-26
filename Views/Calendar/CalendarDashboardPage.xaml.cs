using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using EHMR.ViewModels;
using EHMR.ViewModels.Calendar;

namespace EHMR.Views;

public partial class CalendarDashboardPage : ContentPage
{
    private readonly CalendarDashboardViewModel _viewModel;
    private readonly MenuView _menuView;

    public CalendarDashboardPage(CalendarDashboardViewModel viewModel, MenuView menuView)
    {
        InitializeComponent();
        _viewModel=viewModel;
        _menuView=menuView;
        BindingContext=_viewModel;
        MenuHost.Content=_menuView;
    }

    // Клик на веќе постоечки настан внатре во дневниот Timeline
    private async void OnTimelineItemBorderTapped(object sender, EventArgs e)
    {
        if(sender is Border border&&border.BindingContext is CalendarEventDto selectedEvent)
        {
            if(BindingContext is CalendarDashboardViewModel viewModel)
            {
                switch(selectedEvent.EventType)
                {
                    case "Encounter":
                        if(viewModel.ProcessEncounterSelectionCommand.CanExecute(selectedEvent.Id))
                            await viewModel.ProcessEncounterSelectionCommand.ExecuteAsync(selectedEvent.Id);
                        break;

                        // За идни типови настани (на пр. "Cycle", "Event") — додади нови case-ови тука
                        // кога воведеш соодветни ViewModel команди.
                }
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if(_viewModel!=null)
        {
            await _viewModel.LoadDashboardDataAsync();
        }
    }
}