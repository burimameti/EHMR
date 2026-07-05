using EHMR.Resources.Controls;
using System.Globalization;


namespace EHMR.Converters
{
    /// <summary>
    /// True when the bound string is non-empty. Used to hide the tab count badge
    /// when Value is null, and to hide icon/label pieces in the composite button
    /// template when IconGlyph or Label isn't provided.
    /// Register once in App.xaml resources:
    ///   &lt;sc:SparkStringNotEmptyConverter x:Key="SparkStringNotEmptyConverter" /&gt;
    /// </summary>
    /// 
    public class SparkNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value!=null;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>True when the bound collection is not null and has at least one item. Used to
    /// hide the DataGridHeaderGrid row entirely when HeaderColumns is empty/unset.
    /// Register: &lt;sc:SparkCollectionNotEmptyConverter x:Key="SparkCollectionNotEmptyConverter" /&gt;</summary>
    public class SparkCollectionNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is System.Collections.ICollection c&&c.Count>0;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
    public class SparkStringNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => !string.IsNullOrEmpty(value as string);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>SparkBadgeTone -> text color (used by metric card values and the profile risk badge).</summary>
    public class SparkToneToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as SparkBadgeTone?) switch
            {
                SparkBadgeTone.Success => Color.FromArgb("#3CB35B"),
                SparkBadgeTone.Danger => Color.FromArgb("#E0554F"),
                _ => Color.FromArgb("#2E3A4E")
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>SparkBadgeTone -> background tint (used by the profile risk badge pill).</summary>
    public class SparkToneToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as SparkBadgeTone?) switch
            {
                SparkBadgeTone.Success => Color.FromArgb("#E7F6E9"),
                SparkBadgeTone.Danger => Color.FromArgb("#FDE8E8"),
                _ => Color.FromArgb("#EDF1F5")
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}