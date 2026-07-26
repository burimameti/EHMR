using System.Globalization;

namespace EHMR.ViewModels.Calendar;


    public class WeekColumnDto
{
    public DateTime Date
    {
        get; set;
    }
    public int DayNumber => Date.Day;
    public string WeekdayLabel => Date.ToString("ddd", new CultureInfo("mk-MK")).ToUpperInvariant();
    public bool IsToday => Date.Date==DateTime.Today;
    public List<CalendarEventDto> Events { get; set; } = new();
    public int EventCount => Events.Count;
}
