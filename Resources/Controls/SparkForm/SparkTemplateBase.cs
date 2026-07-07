namespace EHMR.Domain.SparkForm;

public abstract class SparkTemplateBase : ISparkFieldTemplate
{
    public abstract View Build(
        SparkFieldContext context);


    protected Entry CreateEntry(
        SparkFieldContext context,
        Keyboard keyboard)
    {
        var entry = new Entry
        {
            Placeholder=context.Placeholder??context.Label,
            Keyboard=keyboard,
            IsReadOnly=context.Field.IsReadOnly
        };


        entry.SetBinding(
            Entry.TextProperty,
            nameof(SparkFormField.Value),
            BindingMode.TwoWay);


        return entry;
    }


    protected Editor CreateEditor(
        SparkFieldContext context)
    {
        var editor = new Editor
        {
            Placeholder=context.Placeholder??context.Label,
            AutoSize=EditorAutoSizeOption.TextChanges,
            IsReadOnly=context.Field.IsReadOnly
        };


        editor.SetBinding(
            Editor.TextProperty,
            nameof(SparkFormField.Value),
            BindingMode.TwoWay);


        return editor;
    }


    protected T BindValue<T>(
        T control,
        BindableProperty property)
        where T : BindableObject
    {
        control.SetBinding(
            property,
            nameof(SparkFormField.Value),
            BindingMode.TwoWay);

        return control;
    }
}

