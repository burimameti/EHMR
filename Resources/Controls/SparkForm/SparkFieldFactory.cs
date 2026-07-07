using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace EHMR.Domain.SparkForm;


public class SparkFieldFactory
{
    public virtual SparkFormField Create(
        PropertyInfo property)
    {
        var nullable =
            Nullable.GetUnderlyingType(property.PropertyType)!=null
            ||
            !property.PropertyType.IsValueType;



        var field = new SparkFormField
        {
            PropertyName=property.Name,

            Label=Humanize(property.Name),

            PropertyType=property.PropertyType,

            FieldType=
                ResolveFieldType(
                    property.PropertyType),

            IsRequired=
                property.GetCustomAttribute<RequiredAttribute>()!=null,

            AllowNull=nullable
        };



        ApplySparkAttribute(
            field,
            property);



        ApplyEnumOptions(
            field,
            property);



        return field;
    }




    protected virtual void ApplySparkAttribute(
        SparkFormField field,
        PropertyInfo property)
    {
        var attr =
            property.GetCustomAttribute<SparkFieldAttribute>();


        if(attr==null)
            return;



        if(!string.IsNullOrWhiteSpace(attr.Label))
            field.Label=attr.Label;



        if(!string.IsNullOrWhiteSpace(attr.Placeholder))
            field.Placeholder=attr.Placeholder;



        field.Order=
            attr.Order;



        field.ColumnSpan=
            attr.ColumnSpan;



        field.Section=
            attr.Section;



        field.Icon=
            attr.Icon;



        field.Format=
            attr.Format;



        field.HelpText=
            attr.HelpText;



        field.IsRequired=
            attr.Required;



        field.IsReadOnly=
            attr.ReadOnly;



        field.IsVisible=
            attr.Visible;



        field.IsEnabled=
            attr.Enabled;



        field.IsSearchable=
            attr.Searchable;



        field.IsLookup=
            attr.Lookup;



        if(attr.FieldType!=SparkFieldType.Auto)
        {
            field.FieldType=
                attr.FieldType;
        }
    }




    protected virtual void ApplyEnumOptions(
        SparkFormField field,
        PropertyInfo property)
    {
        var type =
            Nullable.GetUnderlyingType(
                property.PropertyType)
            ??
            property.PropertyType;



        if(!type.IsEnum)
            return;



        field.Options=
            Enum.GetNames(type)
                .Select(x =>
                    new SparkFieldOption
                    {
                        Value=x,
                        Text=x
                    })
                .ToList();
    }





    protected virtual SparkFieldType ResolveFieldType(
        Type type)
    {
        type=
            Nullable.GetUnderlyingType(type)
            ??
            type;



        if(type==typeof(string))
            return SparkFieldType.Text;



        if(type==typeof(bool))
            return SparkFieldType.Toggle;



        if(type==typeof(DateTime))
            return SparkFieldType.DateTime;



        if(type==typeof(DateOnly))
            return SparkFieldType.Date;



        if(type==typeof(TimeOnly))
            return SparkFieldType.Time;



        if(type==typeof(int))
            return SparkFieldType.Integer;



        if(type==typeof(long))
            return SparkFieldType.Long;



        if(type==typeof(decimal))
            return SparkFieldType.Currency;



        if(type==typeof(double))
            return SparkFieldType.Double;



        if(type.IsEnum)
            return SparkFieldType.Combo;



        return SparkFieldType.Text;
    }




    private static string Humanize(
        string value)
    {
        return string.Concat(
            value.Select((x, i) =>
                i>0&&char.IsUpper(x)
                    ? " "+x
                    : x.ToString()));
    }
}