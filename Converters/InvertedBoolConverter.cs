using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Converters
{
    public class PatientStatusToBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is not PatientStatus status)
                return "#E5E7EB";

            return status switch
            {
                PatientStatus.Active => "#D1FAE5",
                PatientStatus.Inactive => "#E5E7EB",
                PatientStatus.Discharged => "#FEE2E2",
                PatientStatus.Deceased => "#E5E7EB",
                PatientStatus.Chronic => "#FEF3C7",
                PatientStatus.Recovered => "#DBEAFE",
                PatientStatus.UnderObservation => "#FFEDD5",
                _ => "#E5E7EB"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                 => throw new NotImplementedException();
    }
    public class GreaterThanOneConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is int i)
                return i>1;

            if(value is double d)
                return d>1;

            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is bool b)
                return !b;

            return true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is bool b)
                return !b;

            return false;
        }
    }
}