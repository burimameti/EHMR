namespace EHMR.Resources.Controls;

public partial class CrudContextCard : ContentView
{
    public CrudContextCard() => InitializeComponent();
    public static readonly BindableProperty CaptionProperty=BindableProperty.Create(nameof(Caption),typeof(string),typeof(CrudContextCard),string.Empty);
    public static readonly BindableProperty ValueProperty=BindableProperty.Create(nameof(Value),typeof(string),typeof(CrudContextCard),string.Empty);
    public static readonly BindableProperty Detail1Property=BindableProperty.Create(nameof(Detail1),typeof(string),typeof(CrudContextCard),string.Empty);
    public static readonly BindableProperty Detail2Property=BindableProperty.Create(nameof(Detail2),typeof(string),typeof(CrudContextCard),string.Empty);
    public static readonly BindableProperty IconGlyphProperty=BindableProperty.Create(nameof(IconGlyph),typeof(string),typeof(CrudContextCard),"\uf007");
    public static readonly BindableProperty IconBackgroundColorProperty=BindableProperty.Create(nameof(IconBackgroundColor),typeof(Color),typeof(CrudContextCard),Color.FromArgb("#F0F7F5"));
    public static readonly BindableProperty IconColorProperty=BindableProperty.Create(nameof(IconColor),typeof(Color),typeof(CrudContextCard),Color.FromArgb("#32B9AA"));
    public static readonly BindableProperty ValueColorProperty=BindableProperty.Create(nameof(ValueColor),typeof(Color),typeof(CrudContextCard),Color.FromArgb("#1E2733"));
    public string Caption { get=>(string)GetValue(CaptionProperty); set=>SetValue(CaptionProperty,value); }
    public string Value { get=>(string)GetValue(ValueProperty); set=>SetValue(ValueProperty,value); }
    public string Detail1 { get=>(string)GetValue(Detail1Property); set=>SetValue(Detail1Property,value); }
    public string Detail2 { get=>(string)GetValue(Detail2Property); set=>SetValue(Detail2Property,value); }
    public string IconGlyph { get=>(string)GetValue(IconGlyphProperty); set=>SetValue(IconGlyphProperty,value); }
    public Color IconBackgroundColor { get=>(Color)GetValue(IconBackgroundColorProperty); set=>SetValue(IconBackgroundColorProperty,value); }
    public Color IconColor { get=>(Color)GetValue(IconColorProperty); set=>SetValue(IconColorProperty,value); }
    public Color ValueColor { get=>(Color)GetValue(ValueColorProperty); set=>SetValue(ValueColorProperty,value); }
}