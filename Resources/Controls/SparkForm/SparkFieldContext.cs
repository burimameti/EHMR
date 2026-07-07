namespace EHMR.Domain.SparkForm;


public enum SparkFieldMode
{
    Create,
    Edit,
    View,
    Search,
    Filter
}



public sealed class SparkFieldContext
{
    // ======================================================
    // FIELD
    // ======================================================

    public required SparkFormField Field
    {
        get;
        init;
    }



    // ======================================================
    // MODE
    // ======================================================

    public required SparkFieldMode Mode
    {
        get;
        init;
    }



    // ======================================================
    // BINDING
    // ======================================================

    public object? BindingContext
    {
        get;
        init;
    }



    // ======================================================
    // DISPLAY
    // ======================================================

    public string Label =>
        Field.Label;



    public string? Placeholder =>
        Field.Placeholder;



    public string? HelpText =>
        Field.HelpText;



    public string Format =>
        Field.Format;



    public string Icon =>
        Field.Icon;



    // ======================================================
    // VALUE
    // ======================================================

    public object? Value =>
        Field.Value;



    public Type ValueType =>
        Field.PropertyType;



    public IReadOnlyList<SparkFieldOption>? Options =>
        Field.Options;



    // ======================================================
    // STATE
    // ======================================================

    public bool IsVisible =>
        Field.IsVisible;



    public bool IsEnabled =>
        Field.IsEnabled;



    public bool IsRequired =>
        Field.IsRequired;



    public bool IsReadOnly =>
        Mode==SparkFieldMode.View
        ||Field.IsReadOnly
        ||!Field.IsEnabled;



    public bool IsEditable =>
        !IsReadOnly;



    // ======================================================
    // VALIDATION
    // ======================================================

    public string? ValidationMessage
    {
        get;
        init;
    }



    public bool HasError =>
        !string.IsNullOrWhiteSpace(
            ValidationMessage);



    public IReadOnlyList<SparkValidationRule> ValidationRules =>
        (IReadOnlyList<SparkValidationRule>)Field.ValidationRules;



    // ======================================================
    // MODE HELPERS
    // ======================================================

    public bool IsSearchMode =>
        Mode==SparkFieldMode.Search;


    public bool IsFilterMode =>
        Mode==SparkFieldMode.Filter;


    public bool IsCreateMode =>
        Mode==SparkFieldMode.Create;


    public bool IsEditMode =>
        Mode==SparkFieldMode.Edit;


    public bool IsViewMode =>
        Mode==SparkFieldMode.View;
}