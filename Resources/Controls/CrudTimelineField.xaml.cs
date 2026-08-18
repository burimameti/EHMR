namespace EHMR.Resources.Controls;

[ContentProperty(nameof(EditContent))]
public partial class CrudTimelineField : ContentView
{
    public static readonly BindableProperty LabelProperty=BindableProperty.Create(nameof(Label), typeof(string), typeof(CrudTimelineField), string.Empty, propertyChanged: OnLabelChanged);
    public static readonly BindableProperty ReadLabelProperty=BindableProperty.Create(nameof(ReadLabel), typeof(string), typeof(CrudTimelineField), string.Empty, propertyChanged: OnLabelChanged);
    public static readonly BindableProperty EffectiveReadLabelProperty=BindableProperty.Create(nameof(EffectiveReadLabel), typeof(string), typeof(CrudTimelineField), string.Empty);
    public static readonly BindableProperty IsEditModeProperty=BindableProperty.Create(nameof(IsEditMode), typeof(bool), typeof(CrudTimelineField));
    public static readonly BindableProperty IsReadOnlyProperty=BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(CrudTimelineField));
    public static readonly BindableProperty EditContentProperty=BindableProperty.Create(nameof(EditContent), typeof(View), typeof(CrudTimelineField), null, propertyChanged: OnEditContentChanged);
    public static readonly BindableProperty ReadContentProperty=BindableProperty.Create(nameof(ReadContent), typeof(View), typeof(CrudTimelineField), null, propertyChanged: OnReadContentChanged);
    public static readonly BindableProperty ShowLineProperty=BindableProperty.Create(nameof(ShowLine), typeof(bool), typeof(CrudTimelineField), true);

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string ReadLabel { get => (string)GetValue(ReadLabelProperty); set => SetValue(ReadLabelProperty, value); }
    public string EffectiveReadLabel { get => (string)GetValue(EffectiveReadLabelProperty); private set => SetValue(EffectiveReadLabelProperty, value); }
    public bool IsEditMode { get => (bool)GetValue(IsEditModeProperty); set => SetValue(IsEditModeProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
    public View? EditContent { get => (View?)GetValue(EditContentProperty); set => SetValue(EditContentProperty, value); }
    public View? ReadContent { get => (View?)GetValue(ReadContentProperty); set => SetValue(ReadContentProperty, value); }
    public bool ShowLine { get => (bool)GetValue(ShowLineProperty); set => SetValue(ShowLineProperty, value); }

    public CrudTimelineField()
    {
        InitializeComponent();
        EditContentHost.Content=EditContent;
        ReadContentHost.Content=ReadContent;
        UpdateEffectiveReadLabel();
    }

    private static void OnEditContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control=(CrudTimelineField)bindable;
        if(control.EditContentHost is not null)
            control.EditContentHost.Content=newValue as View;
    }

    private static void OnReadContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control=(CrudTimelineField)bindable;
        if(control.ReadContentHost is not null)
            control.ReadContentHost.Content=newValue as View;
    }

    private static void OnLabelChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CrudTimelineField)bindable).UpdateEffectiveReadLabel();

    private void UpdateEffectiveReadLabel()
    {
        var label=string.IsNullOrWhiteSpace(ReadLabel) ? Label.TrimEnd(' ', '*') : ReadLabel;
        EffectiveReadLabel=label.EndsWith(':') ? label : $"{label}:";
    }
}