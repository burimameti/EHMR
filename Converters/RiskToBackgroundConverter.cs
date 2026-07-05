// EHMR.UI/Converters/RiskConverters.cs
using EHMR.Resources.Theming;
using System.Globalization;

namespace EHMR.Converters;
public class LayoutStyleToTemplateConverter : IValueConverter
{
    public DataTemplate ClassicTemplate
    {
        get; set;
    }
    public DataTemplate SparkTemplate
    {
        get; set;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (FFThemeVariant)value== FFThemeVariant.Sparked ? SparkTemplate : ClassicTemplate;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
public class RiskToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is not double and not int) return Colors.Transparent;
        double risk = System.Convert.ToDouble(value);

        return risk>=50
            ? Color.FromArgb("#FBE3E4")   // soft red
            : Color.FromArgb("#E0F5DE");  // soft green
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class RiskToTextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is not double and not int) return Colors.Black;
        double risk = System.Convert.ToDouble(value);

        if(risk>=50) return Color.FromArgb("#C0392B");   // strong red
        if(risk>=10) return Color.FromArgb("#3E9142");   // medium green
        return Color.FromArgb("#1F7A2E");                   // dark green (very low risk)
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Compares CurrentPage (int) to the page number in the item (int) for pill highlight
public class PageActiveConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if(values.Length<2||values[0] is not int current||values[1] is not int page)
            return false;
        return current==page;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}