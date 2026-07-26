//using CommunityToolkit.Mvvm.ComponentModel;

//namespace EHMR.ViewModels.Calendar;


//    public partial class CalendarDayDto : ObservableObject
//{
//    public DateTime Date
//    {
//        get; set;
//    }
//    public int DayNumber => Date.Day;
//    public bool IsEmptySlot
//    {
//        get; set;
//    }
//    public bool IsCurrentMonth
//    {
//        get; set;
//    }
//    public bool IsToday => Date.Date==DateTime.Today;
//    public int EventCount => Events.Count;
//    public List<CalendarEventDto> Events { get; set; } = new();
//    public List<CalendarEventDto> VisibleEvents => Events.Take(3).ToList();
//    public bool HasHiddenEvents => Events.Count>3;
//    public int HiddenEventsCount => Math.Max(Events.Count-3, 0);

//    public int WeekIndex
//    {
//        get; set;
//    }

//   [ObservableProperty] private bool isWeekHighlighted;
//}
