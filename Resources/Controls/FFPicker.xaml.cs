using System.Collections;
using Microsoft.Maui.Controls;
namespace EHMR.Resources.Controls;
public partial class FFPicker : ContentView
{
    public static readonly BindableProperty ResponsiveScaleProperty =
        BindableProperty.Create(nameof(ResponsiveScale), typeof(double), typeof(FFPicker), 1d,
            propertyChanged: (b, _, n) => ((FFPicker)b).ApplyResponsiveScale((double)n));

    public double ResponsiveScale
    {
        get => (double)GetValue(ResponsiveScaleProperty);
        set => SetValue(ResponsiveScaleProperty, value);
    }

    private void ApplyResponsiveScale(double scale)
    {
        scale=Math.Clamp(scale, 0.72, 1.0);
        InnerPicker.FontSize=13*scale;
        InnerPicker.Margin=new Thickness(10*scale, 0, 30*scale, 0);
        ArrowButton.Margin=new Thickness(0, 5*scale, 5*scale, 5*scale);
        ArrowButton.WidthRequest=31*scale;
        ArrowButton.HeightRequest=32*scale;
    }

    public FFPicker()
    {
        InitializeComponent();
        InnerPicker.SelectedIndexChanged+=(s, e) =>
        {
            if(InnerPicker.SelectedIndex!=SelectedIndex)
                SelectedIndex=InnerPicker.SelectedIndex;
        };
    }

    private void OnArrowTapped(object? sender, TappedEventArgs e)
    {
#if WINDOWS
        if(InnerPicker.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ComboBox comboBox)
        {
            comboBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            comboBox.IsDropDownOpen=true;
            return;
        }
#endif
        InnerPicker.Focus();
    }

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(FFPicker), string.Empty);
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FFPicker), string.Empty);
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(FFPicker), null,
            propertyChanged: (b, _, n) =>
            {
                if(b is FFPicker p)
                    p.InnerPicker.ItemsSource=(IList)n;
            });
    public IList ItemsSource
    {
        get => (IList)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty ItemDisplayBindingProperty =
        BindableProperty.Create(nameof(ItemDisplayBinding), typeof(BindingBase), typeof(FFPicker), null,
            propertyChanged: (b, _, n) =>
            {
                if(b is FFPicker p)
                    p.InnerPicker.ItemDisplayBinding=(BindingBase)n;
            });
    public BindingBase ItemDisplayBinding
    {
        get => (BindingBase)GetValue(ItemDisplayBindingProperty);
        set => SetValue(ItemDisplayBindingProperty, value);
    }

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(FFPicker), null, BindingMode.TwoWay);
    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(FFPicker), -1, BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                if(b is FFPicker p&&p.InnerPicker.SelectedIndex!=(int)n)
                    p.InnerPicker.SelectedIndex=(int)n;
            });
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }
}