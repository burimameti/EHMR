using EHMR.ViewModels;
using EHMR.ViewModels.Calendar;
using System.Globalization;

namespace EHMR.Extensions;

public class CountToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count&&count>0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
public class DayBoundsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is CalendarEventDto evt)
        {
            double startHour = 8.0; // Графиката почнува од 08:00 AM
            double eventStart = evt.ScheduledTime.Hour+(evt.ScheduledTime.Minute/60.0);

            // 60px висина по час
            double top = (eventStart-startHour)*60.0;
            if(top<0) top=0;

            double height = (evt.Duration.TotalMinutes/60.0)*60.0;
            if(height<40) height=40; // Минимална висина за чист текст

            return new Rect(0, top, 1, height);
        }
        return new Rect(0, 0, 1, 45);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}