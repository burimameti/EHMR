namespace EHMR.Resources.Controls;

[ContentProperty(nameof(RowContent))]
public partial class CrudTimelineRow : ContentView
{
    public static readonly BindableProperty RowContentProperty=BindableProperty.Create(
        nameof(RowContent), typeof(View), typeof(CrudTimelineRow), null,
        propertyChanged: OnRowContentChanged);

    public static readonly BindableProperty ShowLineProperty=BindableProperty.Create(
        nameof(ShowLine), typeof(bool), typeof(CrudTimelineRow), true);

    public View? RowContent
    {
        get => (View?)GetValue(RowContentProperty);
        set => SetValue(RowContentProperty, value);
    }

    public bool ShowLine
    {
        get => (bool)GetValue(ShowLineProperty);
        set => SetValue(ShowLineProperty, value);
    }

    public CrudTimelineRow()
    {
        InitializeComponent();
        RowContentHost.Content=RowContent;
    }

    private static void OnRowContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control=(CrudTimelineRow)bindable;
        if(control.RowContentHost is not null)
            control.RowContentHost.Content=newValue as View;
    }
}