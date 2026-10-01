using System.Collections;
using Microsoft.Maui.Controls;

namespace EHMR.Resources.Controls;

public partial class FFPicker2 : ContentView
{
    public FFPicker2()
    {
        InitializeComponent();

        InnerPicker.SelectedIndexChanged += (_, _) =>
        {
            if (!Equals(GetValue(SelectedIndexProperty), InnerPicker.SelectedIndex))
                SetValue(SelectedIndexProperty, InnerPicker.SelectedIndex);

            var selected = InnerPicker.SelectedItem;
            if (!Equals(GetValue(SelectedItemProperty), selected))
                SetValue(SelectedItemProperty, selected);
        };

        SizeChanged += (_, _) =>
        {
            if (Width <= 0)
                return;

            InnerPicker.FontSize = Width < 260 ? 12 : 13;
            InnerPicker.Margin = Width < 260
                ? new Thickness(7, 0, 5, 0)
                : new Thickness(10, 0, 8, 0);
        };
    }

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(FFPicker2), string.Empty);

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FFPicker2), "Изберете");

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IList),
            typeof(FFPicker2),
            null,
            propertyChanged: (b, _, n) =>
            {
                if (b is FFPicker2 picker)
                {
                    picker.InnerPicker.ItemsSource = n as IList;
                    picker.SyncSelection();
                }
            });

    public IList ItemsSource
    {
        get => (IList)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty ItemDisplayBindingProperty =
        BindableProperty.Create(
            nameof(ItemDisplayBinding),
            typeof(BindingBase),
            typeof(FFPicker2),
            null,
            propertyChanged: (b, _, n) =>
            {
                if (b is FFPicker2 picker)
                    picker.InnerPicker.ItemDisplayBinding = n as BindingBase;
            });

    public BindingBase ItemDisplayBinding
    {
        get => (BindingBase)GetValue(ItemDisplayBindingProperty);
        set => SetValue(ItemDisplayBindingProperty, value);
    }

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(
            nameof(SelectedItem),
            typeof(object),
            typeof(FFPicker2),
            null,
            BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                if (b is FFPicker2 picker &&
                    !Equals(picker.InnerPicker.SelectedItem, n))
                {
                    picker.InnerPicker.SelectedItem = n;
                }
            });

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(
            nameof(SelectedIndex),
            typeof(int),
            typeof(FFPicker2),
            -1,
            BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                if (b is FFPicker2 picker &&
                    picker.InnerPicker.SelectedIndex != (int)n)
                {
                    picker.InnerPicker.SelectedIndex = (int)n;
                }
            });

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    private void SyncSelection()
    {
        var selected = GetValue(SelectedItemProperty);

        if (selected is not null &&
            ItemsSource is IList items &&
            items.Contains(selected))
        {
            InnerPicker.SelectedItem = selected;
            return;
        }

        if (selected is null)
        {
            InnerPicker.SelectedIndex = -1;
            SetValue(SelectedIndexProperty, -1);
        }
    }
}