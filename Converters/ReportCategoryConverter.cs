using EHMR.Domain.Entities.Reports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Converters
{
    public class ReportCategoryConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is ReportCategory category)
            {
                return category switch
                {
                    ReportCategory.Clinical => "Клинички",
                    ReportCategory.Operational => "Оперативни",
                    ReportCategory.Security => "Безбедност",
                    _ => category.ToString()
                };
            }

            return value?.ToString()??string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
