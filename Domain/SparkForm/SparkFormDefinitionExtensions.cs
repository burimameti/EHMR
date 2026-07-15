using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Interfaces;
using EHMR.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace EHMR.Domain.SparkForm;

public static class SparkFormDefinitionExtensions
{
    public static void ApplyTo(this SparkFormDefinition form, object entity)
    {
        if(entity==null) return;

        foreach(var field in form.Fields)
        {
            if(field.IsReadOnly) continue;

            var property = entity.GetType().GetProperty(field.PropertyName);
            if(property==null||!property.CanWrite) continue;

            var value = ConvertValue(field.Value, property.PropertyType);
            property.SetValue(entity, value);
        }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if(value==null) return null;

        var underlying = Nullable.GetUnderlyingType(targetType)??targetType;

        if(underlying.IsInstanceOfType(value))
            return value;

        if(underlying.IsEnum)
            return value is string s ? Enum.Parse(underlying, s) : Enum.ToObject(underlying, value);

        return Convert.ChangeType(value, underlying);
    }
}
