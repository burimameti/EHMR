// EHMR/Converters/DocumentIsImageConverter.cs
using EHMR.Services.Dto;
using EHMR.ViewModels;
using System.Globalization;

namespace EHMR.Converters;

public class DocumentIsImageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isImage = value is PatientDocumentDto dto&&PatientDetailFormViewModel.IsImageDocument(dto);

        if(parameter is string p&&p.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            return !isImage;

        return isImage;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}