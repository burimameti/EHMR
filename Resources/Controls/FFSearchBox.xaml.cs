using Microsoft.Maui.Controls;
using System;

namespace EHMR.Resources.Controls;
public partial class FFSearchBox : ContentView
{
    public event EventHandler<TextChangedEventArgs> SearchTextChanged;
    public FFSearchBox()
    {
        InitializeComponent();
    }
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FFSearchBox), string.Empty, BindingMode.TwoWay);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FFSearchBox), "Search...");
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(FFSearchBox), default(string));
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly BindableProperty HintProperty =
        BindableProperty.Create(nameof(Hint), typeof(string), typeof(FFSearchBox), default(string));
    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        // Го пренесуваме настанот нагоре до страницата која ја користи контролата
        SearchTextChanged?.Invoke(this, e);
    }
}