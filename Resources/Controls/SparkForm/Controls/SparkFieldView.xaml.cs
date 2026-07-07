using EHMR.Domain.SparkForm;

namespace EHMR.Resources.Controls.SparkForms.Controls;

public partial class SparkFieldView : ContentView
{
    private SparkFieldContext? _context;

    public SparkFieldView()
    {
        InitializeComponent();
    }

    // ======================================================
    // FIELD
    // ======================================================

    public static readonly BindableProperty FieldProperty =
        BindableProperty.Create(
            nameof(Field),
            typeof(SparkFormField),
            typeof(SparkFieldView),
            null,
            propertyChanged: OnFieldChanged);

    public SparkFormField? Field
    {
        get => (SparkFormField?)GetValue(FieldProperty);
        set => SetValue(FieldProperty, value);
    }

    // ======================================================
    // MODE
    // ======================================================

    public static readonly BindableProperty ModeProperty =
        BindableProperty.Create(
            nameof(Mode),
            typeof(SparkFieldMode),
            typeof(SparkFieldView),
            SparkFieldMode.Create,
            propertyChanged: OnModeChanged);

    public SparkFieldMode Mode
    {
        get => (SparkFieldMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    // ======================================================
    // EVENTS
    // ======================================================

    public event EventHandler? FieldBuilt;

    public event EventHandler? ValueChanged;

    public event EventHandler? ValidationChanged;

    // ======================================================
    // PROPERTY CHANGED
    // ======================================================

    private static void OnFieldChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        ((SparkFieldView)bindable).Build();
    }

    private static void OnModeChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        ((SparkFieldView)bindable).Build();
    }

    // ======================================================
    // BUILD
    // ======================================================

    private void Build()
    {
        PART_ControlHost.Content=null;

        if(Field==null)
            return;

        _context=CreateContext();

        BindingContext=_context;

        ApplyState();

        var control = BuildControl();

        PART_ControlHost.Content=control;

        ApplyBindings(control);

        ApplyMode(control);

        ApplyValidation();

        ApplyHelpText();

        RaiseBuilt();
    }

    // ======================================================
    // CONTEXT
    // ======================================================

    private SparkFieldContext CreateContext()
    {
        return new SparkFieldContext
        {
            Field=Field!,
            Mode=Mode,
            BindingContext=BindingContext
        };
    }

    // ======================================================
    // TEMPLATE
    // ======================================================

    private ISparkFieldTemplate? ResolveTemplate()
    {
        return SparkTemplateRegistry.Resolve(
            Field!.FieldType);
    }

    private View BuildControl()
    {
        var template = ResolveTemplate();

        if(template==null)
        {
            return new Label
            {
                Text=$"Missing template : {Field!.FieldType}",
                TextColor=Colors.Red
            };
        }

        return template.Build(_context!);
    }

    // ======================================================
    // STATE
    // ======================================================

    private void ApplyState()
    {
        if(Field==null)
            return;

        IsVisible=Field.IsVisible;

        base.IsEnabled=
            Field.IsEnabled;
    }

    // ======================================================
    // MODE
    // ======================================================

    private void ApplyMode(View control)
    {
        if(Field==null)
            return;

        bool readOnly =
            Mode==SparkFieldMode.View||
            Field.IsReadOnly;

        switch(control)
        {
            case Entry entry:
                entry.IsReadOnly=readOnly;
                break;

            case Editor editor:
                editor.IsReadOnly=readOnly;
                break;

            case Picker picker:
                picker.IsEnabled=!readOnly;
                break;

            case DatePicker picker:
                picker.IsEnabled=!readOnly;
                break;

            case TimePicker picker:
                picker.IsEnabled=!readOnly;
                break;

            case CheckBox check:
                check.IsEnabled=!readOnly;
                break;

            case Switch toggle:
                toggle.IsEnabled=!readOnly;
                break;
        }
    }

    // ======================================================
    // BINDINGS
    // ======================================================

    private void ApplyBindings(View control)
    {
        if(Field==null)
            return;

        switch(control)
        {
            case Entry entry:
                entry.Text=Field.Value?.ToString();
                break;

            case Editor editor:
                editor.Text=Field.Value?.ToString();
                break;
        }
    }

    // ======================================================
    // VALIDATION
    // ======================================================

    private void ApplyValidation()
    {
        if(Field==null)
            return;

        PART_RequiredLabel.IsVisible=
            Field.IsRequired;

        PART_ErrorLabel.IsVisible=
            Field.HasError;

        PART_ErrorLabel.Text=
            Field.ValidationMessage;
    }

    // ======================================================
    // HELP
    // ======================================================

    private void ApplyHelpText()
    {
        if(Field==null)
            return;

        PART_HelpLabel.IsVisible=
            !string.IsNullOrWhiteSpace(
                Field.HelpText);

        PART_HelpLabel.Text=
            Field.HelpText;
    }

    // ======================================================
    // REFRESH
    // ======================================================

    public void Refresh()
    {
        ApplyState();

        RefreshValue();

        RefreshValidation();
    }

    public void RefreshValue()
    {
        if(Field==null)
            return;

        switch(PART_ControlHost.Content)
        {
            case Entry entry:
                entry.Text=Field.Value?.ToString();
                break;

            case Editor editor:
                editor.Text=Field.Value?.ToString();
                break;
        }
    }

    public void RefreshValidation()
    {
        ApplyValidation();

        ValidationChanged?.Invoke(
            this,
            EventArgs.Empty);
    }

    public void Rebuild()
    {
        Build();
    }

    // ======================================================
    // API
    // ======================================================

    public void SetValue(object? value)
    {
        if(Field==null)
            return;

        Field.Value=value;

        RefreshValue();

        ValueChanged?.Invoke(
            this,
            EventArgs.Empty);
    }

    public bool Validate()
    {
        RefreshValidation();

        return Field==null||
               !Field.HasError;
    }

    public void ClearValidation()
    {
        if(Field==null)
            return;

        Field.ValidationMessage=null;

        RefreshValidation();
    }

    public void FocusField()
    {
        PART_ControlHost.Content?.Focus();
    }

    // ======================================================
    // EVENTS
    // ======================================================

    private void RaiseBuilt()
    {
        FieldBuilt?.Invoke(
            this,
            EventArgs.Empty);
    }
}