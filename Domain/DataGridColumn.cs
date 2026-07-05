namespace EHMR.Domain;

public class DataGridColumn<T>
{
    public string Key { get; set; } = "";
    public string Header { get; set; } = "";
    public double Width { get; set; } = 120;
    public bool Sortable { get; set; } = true;

    // RAW VALUE
    public Func<T, object?> Value { get; set; } = _ => null;

    // DISPLAY PIPELINE
    public Func<object?, string>? Display
    {
        get; set;
    }

    // OPTIONAL OVERRIDE (future template engine)
    public DataTemplate? Template
    {
        get; set;
    }
}
