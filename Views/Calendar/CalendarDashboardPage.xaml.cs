using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using EHMR.ViewModels;

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

    // Единствен влез за селекција на ден од календарот
    private void OnCalendarDaySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(e.CurrentSelection.FirstOrDefault() is CalendarDayDto selectedDay)
        {
            if(selectedDay.IsIsEmptySlot)
            {
                // Спречи селекција на празни полиња
                ((CollectionView)sender).SelectedItem=null;
                return;
            }

            // Повикај ја функцијата од ViewModel за да ја наполни саатницата за тој ден
            if(BindingContext is CalendarDashboardViewModel viewModel)
            {
                viewModel.SelectedCalendarDay=selectedDay;
                viewModel.IsDayViewActive=true;
                viewModel.IsStatsViewActive=false;

                // Ова ја активира магијата за полнење на десниот панел по часови!
                viewModel.BuildHourlyTimeline(selectedDay);
            }
        }
    }

    // Клик на веќе постоечки настан внатре во дневниот Timeline
    private async void OnTimelineItemBorderTapped(object sender, EventArgs e)
    {
        if(sender is Border border&&border.BindingContext is CalendarEventDto selectedEvent)
        {
            if(BindingContext is CalendarDashboardViewModel viewModel)
            {
                viewModel.SelectedCalendarEvent=selectedEvent;

                switch(selectedEvent.EventType)
                {
                    case "Cycle":
                        if(viewModel.ProcessCycleSelectionCommand.CanExecute(selectedEvent.Id))
                            await viewModel.ProcessCycleSelectionCommand.ExecuteAsync(selectedEvent.Id);
                        break;

                    case "Appointment":
                        if(viewModel.ProcessAppointmentSelectionCommand.CanExecute(selectedEvent.Id))
                            await viewModel.ProcessAppointmentSelectionCommand.ExecuteAsync(selectedEvent.Id);
                        break;

                    case "Event":
                    default:
                        if(viewModel.ProcessGenericEventSelectionCommand.CanExecute(selectedEvent.Id))
                            await viewModel.ProcessGenericEventSelectionCommand.ExecuteAsync(selectedEvent.Id);
                        break;
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