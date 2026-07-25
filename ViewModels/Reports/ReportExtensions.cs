using EHMR.Domain.Entities;
using EHMR.Resources.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Reports
{

    public static class PickerLocalizationExtensions
    {
        private static readonly Dictionary<string, string> FormatLabels = new()
        {
            ["All"]="Сите",
            ["PDF"]="PDF",
            ["Excel"]="Excel"
        };

        private static readonly Dictionary<string, string> StatusLabels = new()
        {
            ["All"]="Сите",
            ["Success"]="Успешно",
            ["Failed"]="Неуспешно"
        };

        private static readonly Dictionary<string, string> UserLabels = new()
        {
            ["All"]="Сите"
        };

        public static string ToMk(this string key, Dictionary<string, string> map)
            => map.TryGetValue(key, out var label) ? label : key;

        public static string FromMk(this string label, Dictionary<string, string> map)
            => map.FirstOrDefault(kv => kv.Value==label).Key is { } k&&!string.IsNullOrEmpty(k) ? k : label;



        // For UserFilters, "All" -> "Сите", everything else stays as-is
      
    }


}
