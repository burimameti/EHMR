using System.Collections;
using Microsoft.Maui.Controls;

namespace EHMR.Resources.Controls;

public partial class FFPicker : ContentView
{
    public FFPicker()
    {
        InitializeComponent();
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
        BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(FFPicker), null);

    public IList ItemsSource
    {
        get => (IList)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty ItemDisplayBindingProperty =
        BindableProperty.Create(
            nameof(ItemDisplayBinding),
            typeof(BindingBase),
            typeof(FFPicker),
            null,
            propertyChanged: OnItemDisplayBindingChanged);

    public BindingBase ItemDisplayBinding
    {
        get => (BindingBase)GetValue(ItemDisplayBindingProperty);
        set => SetValue(ItemDisplayBindingProperty, value);
    }

    private static void OnItemDisplayBindingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FFPicker picker)
            picker.InnerPicker.ItemDisplayBinding = (BindingBase)newValue;
    }

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(FFPicker), null, BindingMode.TwoWay);

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(FFPicker), -1, BindingMode.TwoWay);

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }
}