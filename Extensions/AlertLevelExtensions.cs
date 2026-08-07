using EHMR.Domain.Entities;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Extensions
{
    public static class AlertLevelExtensions
    {
        public static string ToLabel(this AlertLevel level) => level switch
        {
            AlertLevel.Critical => "Критични",
            AlertLevel.Warning => "Предупредувања",
            AlertLevel.Info => "Информативни",
            _ => level.ToString()
        };

        public static string ToIcon(this AlertLevel level) => level switch
        {
            AlertLevel.Critical => "\uf071", // triangle-exclamation
            AlertLevel.Warning => "\uf06a", // circle-exclamation
            AlertLevel.Info => "\uf05a", // circle-info
            _ => "\uf05a"
        };

        public static Color ToAccentColor(this AlertLevel level) => level switch
        {
            AlertLevel.Critical => Color.FromArgb("#DC2626"),
            AlertLevel.Warning => Color.FromArgb("#D97706"),
            AlertLevel.Info => Color.FromArgb("#2563EB"),
            _ => Color.FromArgb("#6B7280")
        };

        public static Color ToBackgroundColor(this AlertLevel level) => level switch
        {
            AlertLevel.Critical => Color.FromArgb("#FEF2F2"),
            AlertLevel.Warning => Color.FromArgb("#FFFBEB"),
            AlertLevel.Info => Color.FromArgb("#EFF6FF"),
            _ => Color.FromArgb("#F3F4F6")
        };
    }
}
