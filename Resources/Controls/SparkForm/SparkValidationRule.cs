using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EHMR.Domain.SparkForm
{


    public interface ISparkFormBuilder
    {
        SparkFormDefinition Build<T>(
            SparkFormMode mode = SparkFormMode.Create);



        SparkFormDefinition Build(
            Type entityType,
            SparkFormMode mode = SparkFormMode.Create);



        SparkFormDefinition Build(
            object entity,
            SparkFormMode mode = SparkFormMode.Edit);
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkIgnoreAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkTabAttribute : Attribute
    {
        public SparkTabAttribute(string tab)
        {
            Tab=tab;
        }

        public string Tab
        {
            get;
        }

        public int Order
        {
            get; set;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkRegexAttribute : Attribute
    {
        public SparkRegexAttribute(string pattern)
        {
            Pattern=pattern;
        }

        public string Pattern
        {
            get;
        }

        public string ErrorMessage
        {
            get; set;
        }
            = "Invalid value.";
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkRangeAttribute : Attribute
    {
        public SparkRangeAttribute(double minimum, double maximum)
        {
            Minimum=minimum;
            Maximum=maximum;
        }

        public double Minimum
        {
            get;
        }

        public double Maximum
        {
            get;
        }

        public string ErrorMessage
        {
            get; set;
        }
            = "Value is outside the allowed range.";
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkDisplayAttribute : Attribute
    {
        public SparkDisplayAttribute(string label)
        {
            Label=label;
        }

        public string Label
        {
            get;
        }

        public string Description { get; set; } = string.Empty;
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkPlaceholderAttribute : Attribute
    {
        public SparkPlaceholderAttribute(string text)
        {
            Text=text;
        }

        public string Text
        {
            get;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkReadOnlyAttribute : Attribute
    {
        public SparkReadOnlyAttribute(bool readOnly = true)
        {
            ReadOnly=readOnly;
        }

        public bool ReadOnly
        {
            get;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkVisibleAttribute : Attribute
    {
        public SparkVisibleAttribute(bool visible)
        {
            Visible=visible;
        }

        public bool Visible
        {
            get;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkOrderAttribute : Attribute
    {
        public SparkOrderAttribute(int order)
        {
            Order=order;
        }

        public int Order
        {
            get;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkLookupAttribute : Attribute
    {
        public Type LookupType { get; set; } = typeof(object);

        public string DisplayMember { get; set; } = "Name";

        public string ValueMember { get; set; } = "Id";

        public SparkLookupMode Mode
        {
            get; set;
        }
            = SparkLookupMode.Popup;

        public bool AllowClear { get; set; } = true;

        public bool AllowSearch { get; set; } = true;
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkRequiredAttribute : Attribute
    {
        public string ErrorMessage
        {
            get; set;
        }
            = "Field is required.";
    }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SparkSectionAttribute : Attribute
    {
        public SparkSectionAttribute(string name)
        {
            Name=name;
        }

        public string Name
        {
            get;
        }

        public string Header { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public int Order
        {
            get; set;
        }

        public SparkSectionLayout Layout
        {
            get; set;
        }
            = SparkSectionLayout.Grid;

        public bool Expanded { get; set; } = true;
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SparkFieldAttribute : Attribute
    {
        public string Label { get; set; } = string.Empty;

        public string Placeholder { get; set; } = string.Empty;

        public string HelpText { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;


        public SparkFieldType FieldType
        {
            get; set;
        }
            = SparkFieldType.Auto;


        public string Format { get; set; } = string.Empty;


        public string Section
        {
            get; set;
        }
            = "General";


        public int Order
        {
            get; set;
        }


        public int ColumnSpan { get; set; } = 1;


        public bool Required
        {
            get; set;
        }

        public bool ReadOnly
        {
            get; set;
        }

        public bool Visible { get; set; } = true;

        public bool Enabled { get; set; } = true;

        public bool AllowNull { get; set; } = true;


        public bool Searchable
        {
            get; set;
        }

        public bool Lookup
        {
            get; set;
        }


        public string LookupEntity
        {
            get; set;
        }
            = string.Empty;
    }

    public sealed class SparkFormResult<T>
    {
        public bool Success
        {
            get; set;
        }


        public T? Entity
        {
            get; set;
        }


        public IList<string> Errors
        {
            get; set;
        }
            = new List<string>();
    }
    public sealed class SparkFormDefinition
    {
        public string Title { get; set; } = string.Empty;

        public string Subtitle { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;


        public SparkFormMode Mode
        {
            get; set;
        }


        public SparkFormStyle Style
        {
            get; set;
        }
            = SparkFormStyle.Spark;


        public object? Entity
        {
            get; set;
        }


        public IList<SparkFormSection> Sections
        {
            get; set;
        }
            = new List<SparkFormSection>();


        public IList<SparkFormButton> ToolbarButtons
        {
            get; set;
        }
            = new List<SparkFormButton>();


        public IList<SparkFormButton> FooterButtons
        {
            get; set;
        }
            = new List<SparkFormButton>();


        public IEnumerable<SparkFormField> Fields =>
            Sections.SelectMany(x => x.Fields);



        public SparkFormField? FindField(string property)
        {
            return Fields.FirstOrDefault(
                x => x.PropertyName==property);
        }
    }
    public class SparkFormButton
    {
        public string Text { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public SparkButtonStyle Style
        {
            get; set;
        }

        public bool IsVisible { get; set; } = true;

        public bool IsEnabled { get; set; } = true;

        public ICommand? Command
        {
            get; set;
        }
    }
    public class SparkFormSection
    {
        public string Name { get; set; } = string.Empty;

        public string Header { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public int Order
        {
            get; set;
        }

        public bool Expanded { get; set; } = true;

        public SparkSectionLayout Layout
        {
            get; set;
        }
            = SparkSectionLayout.Grid;

        public IList<SparkFormField> Fields
        {
            get; set;
        }
            = new List<SparkFormField>();
    }



    public sealed class SparkFormField
    {
        public string PropertyName { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Placeholder { get; set; } = string.Empty;

        public string Section { get; set; } = "General";


        public Type PropertyType { get; set; } = typeof(string);


        public SparkFieldType FieldType
        {
            get; set;
        }


        public object? Value
        {
            get; set;
        }


        public SparkFieldState State
        {
            get; set;
        }
            = SparkFieldState.Normal;



        public bool IsVisible { get; set; } = true;

        public bool IsEnabled { get; set; } = true;

        public bool IsReadOnly
        {
            get; set;
        }


        public bool IsRequired
        {
            get; set;
        }


        public bool AllowNull { get; set; } = true;



        public IReadOnlyList<SparkFieldOption>? Options
        {
            get; set;
        }


        public bool IsSearchable
        {
            get; set;
        }

        public bool IsLookup
        {
            get; set;
        }



        public string Format { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public string HelpText { get; set; } = string.Empty;



        public int Order
        {
            get; set;
        }

        public int ColumnSpan { get; set; } = 1;



        public IList<SparkValidationRule> ValidationRules
        {
            get; set;
        }
            = new List<SparkValidationRule>();


        public string? ValidationMessage
        {
            get; set;
        }


        public bool HasError =>
            !string.IsNullOrWhiteSpace(ValidationMessage);
    }
    public sealed class SparkFieldOption
    {
        /// <summary>
        /// Actual value stored in the entity property
        /// Example: Id, Code, Enum value
        /// </summary>
        public object? Value
        {
            get; set;
        }


        /// <summary>
        /// Text displayed to user
        /// Example: "Active", "Doctor", "Germany"
        /// </summary>
        public string Text
        {
            get; set;
        }
            = string.Empty;


        /// <summary>
        /// Optional original object
        /// Example: PatientDto, CountryDto, DepartmentDto
        /// </summary>
        public object? Tag
        {
            get; set;
        }


        /// <summary>
        /// Optional disabled option support
        /// </summary>
        public bool IsEnabled
        {
            get; set;
        }
            = true;


        /// <summary>
        /// Optional grouping
        /// Example: Country group, department group
        /// </summary>
        public string? Group
        {
            get; set;
        }


        public override string ToString()
        {
            return Text;
        }
    }
    public class SparkValidationRule
    {
        public SparkValidationType ValidationType
        {
            get; set;
        }

        public string Message { get; set; } = string.Empty;

        public object? Parameter
        {
            get; set;
        }

        public bool Enabled { get; set; } = true;
    }
}
