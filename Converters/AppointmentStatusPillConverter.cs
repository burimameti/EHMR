using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

namespace EHMR.Converters
{
    /// <summary>
    /// Maps AppointmentStatus → pill background color (semantic strong palette)
    /// </summary>
    public class AppointmentStatusToPillBgConverter : IValueConverter, IMarkupExtension
    {
        private static readonly Dictionary<AppointmentStatus, Color> _colors = new()
        {
            { AppointmentStatus.Scheduled,   Color.FromArgb("#3B82F6") }, // blue
            { AppointmentStatus.InProgress,  Color.FromArgb("#2563EB") }, // deep blue
            { AppointmentStatus.Completed,   Color.FromArgb("#10B981") }, // green
            { AppointmentStatus.Cancelled,   Color.FromArgb("#EF4444") }, // red
            { AppointmentStatus.Missed,      Color.FromArgb("#DC2626") }, // dark red
        };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is AppointmentStatus status&&
                _colors.TryGetValue(status, out var color))
                return color;

            return Color.FromArgb("#F1F5F9"); // neutral gray fallback
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        public IMarkupExtension ProvideValue(IServiceProvider serviceProvider) => this;
        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => this;
    }

    /// <summary>
    /// Maps AppointmentStatus → pill foreground color (ALWAYS contrast-safe)
    /// </summary>
    public class AppointmentStatusToPillFgConverter : IValueConverter, IMarkupExtension
    {
        private static readonly Color Foreground = Color.FromArgb("#FFFFFF"); // always white

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // keep logic simple: foreground is not semantic
            if(value is AppointmentStatus)
                return Foreground;

            return Foreground;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        public IMarkupExtension ProvideValue(IServiceProvider serviceProvider) => this;
        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => this;
    }
}