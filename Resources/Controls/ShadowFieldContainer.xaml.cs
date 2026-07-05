namespace EHMR.Resources.Controls;

public partial class ShadowFieldContainer : ContentView
{
    public ShadowFieldContainer()
    {
        InitializeComponent();
    }

    // ================= LABEL =================
    public static readonly BindableProperty LabelTextProperty =
        BindableProperty.Create(nameof(LabelText), typeof(string),
            typeof(ShadowFieldContainer), string.Empty);

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    // ================= FIELD CONTENT =================
    public static readonly BindableProperty FieldContentProperty =
        BindableProperty.Create(nameof(FieldContent), typeof(View),
            typeof(ShadowFieldContainer));

    public View FieldContent
    {
        get => (View)GetValue(FieldContentProperty);
        set => SetValue(FieldContentProperty, value);
    }
}