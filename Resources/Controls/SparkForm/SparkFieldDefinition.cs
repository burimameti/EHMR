namespace EHMR.Domain.SparkForm;




public sealed class SparkFieldDefinition
{
    // ==========================================
    // IDENTITY
    // ==========================================

    public string Name
    {
        get;
        set;
    }
    = string.Empty;



    public string Label
    {
        get;
        set;
    }
    = string.Empty;



    public string? Description
    {
        get;
        set;
    }



    // ==========================================
    // FIELD TYPE
    // ==========================================

    public SparkFieldType Type
    {
        get;
        set;
    }
    = SparkFieldType.Auto;



    public Type DataType
    {
        get;
        set;
    }
    = typeof(string);



    // ==========================================
    // VALUE
    // ==========================================

    public object? Value
    {
        get;
        set;
    }



    // ==========================================
    // UI
    // ==========================================

    public string? Placeholder
    {
        get;
        set;
    }



    public string? Format
    {
        get;
        set;
    }



    public int ColumnSpan
    {
        get;
        set;
    }
    = 1;



    public int Order
    {
        get;
        set;
    }



    public string Section
    {
        get;
        set;
    }
    = "General";



    public string Group
    {
        get;
        set;
    }
    = string.Empty;



    // ==========================================
    // STATE
    // ==========================================

    public bool Required
    {
        get;
        set;
    }



    public bool ReadOnly
    {
        get;
        set;
    }



    public bool Visible
    {
        get;
        set;
    }
    = true;



    public bool Enabled
    {
        get;
        set;
    }
    = true;



    // ==========================================
    // LOOKUP / OPTIONS
    // ==========================================

    public IReadOnlyList<SparkFieldOption>? Options
    {
        get;
        set;
    }



    public Func<object, string>? DisplaySelector
    {
        get;
        set;
    }



    public Func<object, object>? ValueSelector
    {
        get;
        set;
    }



    // ==========================================
    // VALIDATION
    // ==========================================

    public IList<SparkValidationRule> ValidationRules
    {
        get;
        set;
    }
    = new List<SparkValidationRule>();



    // ==========================================
    // CONDITIONAL
    // ==========================================

    public Func<object?, bool>? VisibleWhen
    {
        get;
        set;
    }



    // ==========================================
    // SECURITY
    // ==========================================

    public bool CanView
    {
        get;
        set;
    }
    = true;



    public bool CanEdit
    {
        get;
        set;
    }
    = true;
}

