using System.Collections;
using System.Reflection;

namespace EHMR.Resources.Controls;

public partial class FFPicker : ContentView
{
    // ---------- Bindable properties ----------

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(FFPicker), string.Empty);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FFPicker), string.Empty,
            propertyChanged: (b, _, _) => ((FFPicker)b).UpdateDisplay());

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(FFPicker), null,
            propertyChanged: (b, _, n) => ((FFPicker)b).OnItemsSourceChanged((IList?)n));

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(FFPicker), null,
            BindingMode.TwoWay,
            propertyChanged: (b, _, n) => ((FFPicker)b).OnSelectedItemChanged(n));

    /// <summary>Property name shown for complex objects (e.g. "Name"). Empty = ToString().</summary>
    public static readonly BindableProperty DisplayMemberPathProperty =
        BindableProperty.Create(nameof(DisplayMemberPath), typeof(string), typeof(FFPicker), string.Empty,
            propertyChanged: (b, _, _) => ((FFPicker)b).OnDisplayMemberPathChanged());

    public string Label
    {
        get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value);
    }
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value);
    }
    public IList? ItemsSource
    {
        get => (IList?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value);
    }
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value);
    }
    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty); set => SetValue(DisplayMemberPathProperty, value);
    }

    bool _syncing;

    public static readonly BindableProperty IsPointerOverProperty =
        BindableProperty.Create(nameof(IsPointerOver), typeof(bool), typeof(FFPicker), false);

    public bool IsPointerOver
    {
        get => (bool)GetValue(IsPointerOverProperty);
        private set => SetValue(IsPointerOverProperty, value);
    }

    public FFPicker()
    {
        InitializeComponent();
        InnerPicker.SelectedIndexChanged+=OnInnerSelectionChanged;
        UpdateDisplay();
    }

    private void OnPointerEntered(object sender, PointerEventArgs e) => IsPointerOver=true;

    private void OnPointerExited(object sender, PointerEventArgs e) => IsPointerOver=false;

    // ---------- Sync ----------

    void OnItemsSourceChanged(IList? items)
    {
        _syncing=true;
        try
        {
            InnerPicker.ItemsSource=items;

            // Native picker clears its selection when the list changes -
            // put the bound value back so it survives late-loaded lists.
            if(SelectedItem is not null&&items is not null)
            {
                var index = IndexOf(items, SelectedItem);
                InnerPicker.SelectedIndex=index;
            }
        }
        finally { _syncing=false; }

        UpdateDisplay();
    }

    void OnSelectedItemChanged(object? item)
    {
        if(!_syncing)
        {
            _syncing=true;
            try
            {
                InnerPicker.SelectedIndex=item is null||ItemsSource is null
                    ? -1
                    : IndexOf(ItemsSource, item);
            }
            finally { _syncing=false; }
        }

        UpdateDisplay();
    }

    void OnInnerSelectionChanged(object? sender, EventArgs e)
    {
        if(_syncing) return;

        // Ignore the native reset-to-null; only real user picks write back.
        if(InnerPicker.SelectedIndex<0||ItemsSource is null) return;

        _syncing=true;
        try { SelectedItem=ItemsSource[InnerPicker.SelectedIndex]; }
        finally { _syncing=false; }

        UpdateDisplay();
    }

    void OnDisplayMemberPathChanged()
    {
        InnerPicker.ItemDisplayBinding=string.IsNullOrWhiteSpace(DisplayMemberPath)
            ? null
            : new Binding(DisplayMemberPath);
        UpdateDisplay();
    }

    // ---------- Display ----------

    void UpdateDisplay()
    {
        var hasValue = SelectedItem is not null;

        ValueLabel.Text=hasValue ? GetDisplayText(SelectedItem!) : Placeholder;
        ValueLabel.FontAttributes=hasValue ? FontAttributes.Bold : FontAttributes.None;
        ValueLabel.TextColor=ResolveColor(hasValue ? "SparkTextPrimary" : "SparkTextMuted");
    }

    string GetDisplayText(object item)
    {
        if(string.IsNullOrWhiteSpace(DisplayMemberPath))
            return item.ToString()??string.Empty;

        var prop = item.GetType().GetProperty(DisplayMemberPath,
            BindingFlags.Public|BindingFlags.Instance);
        return prop?.GetValue(item)?.ToString()??item.ToString()??string.Empty;
    }

    static int IndexOf(IList items, object item)
    {
        var i = items.IndexOf(item);
        if(i>=0) return i;

        // Fallback for equal-by-value items that are different instances.
        for(var k = 0; k<items.Count; k++)
            if(Equals(items[k], item)) return k;
        return -1;
    }

    static Color ResolveColor(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var v)==true&&v is Color c
            ? c
            : Colors.Gray;
}