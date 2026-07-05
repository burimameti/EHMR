namespace EHMR.Resources.Controls;

public partial class ShadowDateField : ShadowFieldContainer
{
    public ShadowDateField()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty DateProperty =
        BindableProperty.Create(nameof(Date), typeof(DateTime),
            typeof(ShadowDateField), DateTime.Today, BindingMode.TwoWay);

    public DateTime Date
    {
        get => (DateTime)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }
}