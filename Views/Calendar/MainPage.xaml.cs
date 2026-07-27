using EHMR.ViewModels;
using EHMR.ViewModels.Calendar;

namespace EHMR.Views.Calendar;

public partial class MainPage : ContentPage
{
    public MainPage(CalendarDashboardViewModel viewModel, MenuView menuView)
    {
        InitializeComponent();

        // Оваа страна го црта неделниот грид со 7 колони и нема дневен flyout,
        // па изборот на ден смее само да го помести означувањето.
        viewModel.UsesWeekTimeline=true;

        BindingContext=viewModel;
        MenuHost.Content=menuView;

        // Постојано видлива лента наместо системскиот scrollbar што се крие.
        WeekGridScrollBar.AttachTo(WeekGridScroll);
        DayGridScrollBar.AttachTo(DayGridScroll);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Автоматско освежување на податоците при секое отворање на страната
        if(BindingContext is CalendarDashboardViewModel vm)
        {
            await vm.LoadDashboardDataAsync();
        }
    }
}