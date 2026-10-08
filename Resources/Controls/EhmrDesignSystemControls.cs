using System.Collections;
using System.Windows.Input;

namespace EHMR.Resources.Controls;

public enum EhmrButtonKind { Primary, Secondary }

public sealed class EhmrButton : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(EhmrButton), "");
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(EhmrButton));
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(EhmrButton));
    public static readonly BindableProperty KindProperty = BindableProperty.Create(nameof(Kind), typeof(EhmrButtonKind), typeof(EhmrButton), EhmrButtonKind.Primary, propertyChanged: (_,__,___) => { });
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty,value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty,value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty,value); }
    public EhmrButtonKind Kind { get => (EhmrButtonKind)GetValue(KindProperty); set => SetValue(KindProperty,value); }
    readonly Button _button = new();
    public EhmrButton()
    {
        _button.SetDynamicResource(StyleProperty, "EhmrButtonPrimaryStyle");
        _button.SetBinding(Button.TextProperty, new Binding(nameof(Text), source:this));
        _button.SetBinding(Button.CommandProperty, new Binding(nameof(Command), source:this));
        _button.SetBinding(Button.CommandParameterProperty, new Binding(nameof(CommandParameter), source:this));
        Content = _button;
        PropertyChanged += (_,e) => { if(e.PropertyName == nameof(Kind)) _button.SetDynamicResource(StyleProperty, Kind == EhmrButtonKind.Primary ? "EhmrButtonPrimaryStyle" : "EhmrButtonSecondaryStyle"); };
    }
}

public sealed class EhmrPicker : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(EhmrPicker), "");
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(EhmrPicker));
    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(EhmrPicker), null, BindingMode.TwoWay);
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty,value); }
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty,value); }
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty,value); }
    public EhmrPicker()
    {
        var label = new Label { StyleClass = new List<string>(), FontSize=11, FontAttributes=FontAttributes.Bold };
        label.SetDynamicResource(Label.TextColorProperty,"EhmrTextSecondary");
        label.SetBinding(Label.TextProperty,new Binding(nameof(Title),source:this));
        var picker = new Picker();
        picker.SetDynamicResource(StyleProperty,"EhmrPickerStyle");
        picker.SetBinding(Picker.ItemsSourceProperty,new Binding(nameof(ItemsSource),source:this));
        picker.SetBinding(Picker.SelectedItemProperty,new Binding(nameof(SelectedItem),source:this,mode:BindingMode.TwoWay));
        Content = FieldFrame(new VerticalStackLayout{Spacing=3,Children={label,picker}});
    }
    static Border FieldFrame(View v) => new(){Content=v,Padding=new Thickness(8,0),BackgroundColor=Colors.White,Stroke=(Color)Application.Current!.Resources["EhmrBorder"],StrokeThickness=1,StrokeShape=new RoundRectangle{CornerRadius=6}};
}

public sealed class EhmrDatePicker : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title),typeof(string),typeof(EhmrDatePicker),"");
    public static readonly BindableProperty DateProperty = BindableProperty.Create(nameof(Date),typeof(DateTime),typeof(EhmrDatePicker),DateTime.Today,BindingMode.TwoWay);
    public static readonly BindableProperty FormatProperty = BindableProperty.Create(nameof(Format),typeof(string),typeof(EhmrDatePicker),"dd.MM.yyyy");
    public string Title { get => (string)GetValue(TitleProperty); set=>SetValue(TitleProperty,value); }
    public DateTime Date { get=>(DateTime)GetValue(DateProperty); set=>SetValue(DateProperty,value); }
    public string Format { get=>(string)GetValue(FormatProperty); set=>SetValue(FormatProperty,value); }
    public EhmrDatePicker()
    {
        var label=new Label{FontSize=11,FontAttributes=FontAttributes.Bold};
        label.SetDynamicResource(Label.TextColorProperty,"EhmrTextSecondary");
        label.SetBinding(Label.TextProperty,new Binding(nameof(Title),source:this));
        var picker=new DatePicker();
        picker.SetDynamicResource(StyleProperty,"EhmrDatePickerStyle");
        picker.SetBinding(DatePicker.DateProperty,new Binding(nameof(Date),source:this,mode:BindingMode.TwoWay));
        picker.SetBinding(DatePicker.FormatProperty,new Binding(nameof(Format),source:this));
        Content=FieldFrame(new VerticalStackLayout{Spacing=3,Children={label,picker}});
    }
    static Border FieldFrame(View v)=>new(){Content=v,Padding=new Thickness(8,0),BackgroundColor=Colors.White,Stroke=(Color)Application.Current!.Resources["EhmrBorder"],StrokeThickness=1,StrokeShape=new RoundRectangle{CornerRadius=6}};
}

public sealed class EhmrInputControl : ContentView
{
    public static readonly BindableProperty LabelProperty=BindableProperty.Create(nameof(Label),typeof(string),typeof(EhmrInputControl),"");
    public static readonly BindableProperty TextProperty=BindableProperty.Create(nameof(Text),typeof(string),typeof(EhmrInputControl),"",BindingMode.TwoWay);
    public static readonly BindableProperty PlaceholderProperty=BindableProperty.Create(nameof(Placeholder),typeof(string),typeof(EhmrInputControl),"");
    public string Label { get=>(string)GetValue(LabelProperty);set=>SetValue(LabelProperty,value); }
    public string Text { get=>(string)GetValue(TextProperty);set=>SetValue(TextProperty,value); }
    public string Placeholder { get=>(string)GetValue(PlaceholderProperty);set=>SetValue(PlaceholderProperty,value); }
    public EhmrInputControl()
    {
        var l=new Label{FontSize=11,FontAttributes=FontAttributes.Bold}; l.SetDynamicResource(Label.TextColorProperty,"EhmrTextSecondary"); l.SetBinding(Label.TextProperty,new Binding(nameof(Label),source:this));
        var e=new Entry(); e.SetDynamicResource(StyleProperty,"EhmrInputStyle"); e.SetBinding(Entry.TextProperty,new Binding(nameof(Text),source:this,mode=BindingMode.TwoWay)); e.SetBinding(Entry.PlaceholderProperty,new Binding(nameof(Placeholder),source:this));
        Content=FieldFrame(new VerticalStackLayout{Spacing=3,Children={l,e}});
    }
    static Border FieldFrame(View v)=>new(){Content=v,Padding=new Thickness(8,0),BackgroundColor=Colors.White,Stroke=(Color)Application.Current!.Resources["EhmrBorder"],StrokeThickness=1,StrokeShape=new RoundRectangle{CornerRadius=6}};
}

public sealed class EhmrSearchControl : ContentView
{
    public static readonly BindableProperty TextProperty=BindableProperty.Create(nameof(Text),typeof(string),typeof(EhmrSearchControl),"",BindingMode.TwoWay);
    public static readonly BindableProperty PlaceholderProperty=BindableProperty.Create(nameof(Placeholder),typeof(string),typeof(EhmrSearchControl),"Search");
    public string Text { get=>(string)GetValue(TextProperty);set=>SetValue(TextProperty,value); }
    public string Placeholder { get=>(string)GetValue(PlaceholderProperty);set=>SetValue(PlaceholderProperty,value); }
    public EhmrSearchControl()
    {
        var s=new SearchBar{HeightRequest=34,FontSize=13,BackgroundColor=Colors.Transparent,CancelButtonColor=Colors.Transparent};
        s.SetDynamicResource(SearchBar.TextColorProperty,"EhmrTextPrimary"); s.SetDynamicResource(SearchBar.PlaceholderColorProperty,"EhmrTextMuted");
        s.SetBinding(SearchBar.TextProperty,new Binding(nameof(Text),source:this,mode=BindingMode.TwoWay)); s.SetBinding(SearchBar.PlaceholderProperty,new Binding(nameof(Placeholder),source:this));
        Content=new Border{Content=s,Padding=new Thickness(8,0),BackgroundColor=Colors.White,Stroke=(Color)Application.Current!.Resources["EhmrBorder"],StrokeThickness=1,StrokeShape=new RoundRectangle{CornerRadius=6}};
    }
}

public sealed class EhmrHeader : ContentView
{
    public static readonly BindableProperty TitleProperty=BindableProperty.Create(nameof(Title),typeof(string),typeof(EhmrHeader),"");
    public static readonly BindableProperty SubtitleProperty=BindableProperty.Create(nameof(Subtitle),typeof(string),typeof(EhmrHeader),"");
    public string Title { get=>(string)GetValue(TitleProperty);set=>SetValue(TitleProperty,value); }
    public string Subtitle { get=>(string)GetValue(SubtitleProperty);set=>SetValue(SubtitleProperty,value); }
    public EhmrHeader()
    {
        var title=new Label{FontSize=22,FontAttributes=FontAttributes.Bold}; title.SetDynamicResource(Label.TextColorProperty,"EhmrTextPrimary"); title.SetBinding(Label.TextProperty,new Binding(nameof(Title),source:this));
        var sub=new Label{FontSize=12}; sub.SetDynamicResource(Label.TextColorProperty,"EhmrTextSecondary"); sub.SetBinding(Label.TextProperty,new Binding(nameof(Subtitle),source:this));
        Content=new Grid{Padding=new Thickness(0,0,0,12),RowDefinitions=new RowDefinitionCollection{new(40),new(24)},Children={title,sub}};
        Grid.SetRow(sub,1);
    }
}

public sealed class EhmrCard : ContentView
{
    public EhmrCard(){ContentChanged();}
    void ContentChanged()
    {
        var inner=Content;
        if(inner is Border) return;
        Content=new Border{Content=inner,BackgroundColor=Colors.White,Stroke=(Color)Application.Current!.Resources["EhmrBorder"],StrokeThickness=1,StrokeShape=new RoundRectangle{CornerRadius=8},Padding=16};
    }
}

public sealed class EhmrFormControl : ContentView
{
    public static readonly BindableProperty TitleProperty=BindableProperty.Create(nameof(Title),typeof(string),typeof(EhmrFormControl),"");
    public string Title { get=>(string)GetValue(TitleProperty);set=>SetValue(TitleProperty,value); }
    public EhmrFormControl()
    {
        var title=new Label{FontSize=14,FontAttributes=FontAttributes.Bold}; title.SetDynamicResource(Label.TextColorProperty,"EhmrTextPrimary"); title.SetBinding(Label.TextProperty,new Binding(nameof(Title),source:this));
        var contentHost=new ContentView(); contentHost.SetBinding(ContentView.ContentProperty,new Binding(nameof(Content),source:this));
        Content=new VerticalStackLayout{Spacing=10,Children={title,contentHost}};
    }
}

public sealed class EhmrGridControl : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty=BindableProperty.Create(nameof(ItemsSource),typeof(IEnumerable),typeof(EhmrGridControl));
    public static readonly BindableProperty ItemTemplateProperty=BindableProperty.Create(nameof(ItemTemplate),typeof(DataTemplate),typeof(EhmrGridControl));
    public IEnumerable? ItemsSource { get=>(IEnumerable?)GetValue(ItemsSourceProperty);set=>SetValue(ItemsSourceProperty,value); }
    public DataTemplate? ItemTemplate { get=>(DataTemplate?)GetValue(ItemTemplateProperty);set=>SetValue(ItemTemplateProperty,value); }
    public EhmrGridControl()
    {
        var list=new CollectionView{SelectionMode=SelectionMode.None,ItemsLayout=new LinearItemsLayout(ItemsLayoutOrientation.Vertical){ItemSpacing=0}};
        list.SetBinding(CollectionView.ItemsSourceProperty,new Binding(nameof(ItemsSource),source:this)); list.SetBinding(CollectionView.ItemTemplateProperty,new Binding(nameof(ItemTemplate),source:this));
        Content=new Border{Content=list,BackgroundColor=Colors.White,Stroke=(Color)Application.Current!.Resources["EhmrBorder"],StrokeThickness=1,StrokeShape=new RoundRectangle{CornerRadius=8}};
    }
}

public sealed class EhmrMenu : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty=BindableProperty.Create(nameof(ItemsSource),typeof(IEnumerable),typeof(EhmrMenu));
    public IEnumerable? ItemsSource { get=>(IEnumerable?)GetValue(ItemsSourceProperty);set=>SetValue(ItemsSourceProperty,value); }
    public EhmrMenu()
    {
        var list=new CollectionView{SelectionMode=SelectionMode.None};
        list.SetBinding(CollectionView.ItemsSourceProperty,new Binding(nameof(ItemsSource),source:this));
        list.ItemTemplate=new DataTemplate(()=>new EhmrMenuItem());
        Content=new Border{Content=list,BackgroundColor=(Color)Application.Current!.Resources["EhmrSidebar"],StrokeThickness=0,Padding=new Thickness(8,12)};
    }
}

public sealed class EhmrMenuItem : ContentView
{
    public static readonly BindableProperty TextProperty=BindableProperty.Create(nameof(Text),typeof(string),typeof(EhmrMenuItem),"");
    public static readonly BindableProperty CommandProperty=BindableProperty.Create(nameof(Command),typeof(ICommand),typeof(EhmrMenuItem));
    public string Text { get=>(string)GetValue(TextProperty);set=>SetValue(TextProperty,value); }
    public ICommand? Command { get=>(ICommand?)GetValue(CommandProperty);set=>SetValue(CommandProperty,value); }
    public EhmrMenuItem()
    {
        var b=new Button{BackgroundColor=Colors.Transparent,Padding=new Thickness(12,8),HorizontalOptions=LayoutOptions.Fill,HorizontalContentAlignment=TextAlignment.Start,HeightRequest=38};
        b.SetDynamicResource(Button.TextColorProperty,"EhmrSurface"); b.SetBinding(Button.TextProperty,new Binding(nameof(Text),source:this)); b.SetBinding(Button.CommandProperty,new Binding(nameof(Command),source:this));
        Content=b;
    }
}

public sealed class EhmrSuggestionsControl : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty=BindableProperty.Create(nameof(ItemsSource),typeof(IEnumerable),typeof(EhmrSuggestionsControl));
    public static readonly BindableProperty ItemTemplateProperty=BindableProperty.Create(nameof(ItemTemplate),typeof(DataTemplate),typeof(EhmrSuggestionsControl));
    public IEnumerable? ItemsSource { get=>(IEnumerable?)GetValue(ItemsSourceProperty);set=>SetValue(ItemsSourceProperty,value); }
    public DataTemplate? ItemTemplate { get=>(DataTemplate?)GetValue(ItemTemplateProperty);set=>SetValue(ItemTemplateProperty,value); }
    public EhmrSuggestionsControl()
    {
        var list=new CollectionView{SelectionMode=SelectionMode.Single,MaximumHeightRequest=240};
        list.SetBinding(CollectionView.ItemsSourceProperty,new Binding(nameof(ItemsSource),source:this)); list.SetBinding(CollectionView.ItemTemplateProperty,new Binding(nameof(ItemTemplate),source:this));
        Content=new Border{Content=list,BackgroundColor=Colors.White,Stroke=(Color)Application.Current!.Resources["EhmrBorder"],StrokeThickness=1,StrokeShape=new RoundRectangle{CornerRadius=6},Padding=8};
    }
}

public sealed class EhmrAdvancedSearchControl : ContentView
{
    public EhmrAdvancedSearchControl()
    {
        var grid=new Grid{ColumnSpacing=8,ColumnDefinitions=new ColumnDefinitionCollection{new(GridLength.Star),new(GridLength.Auto),new(GridLength.Auto)}};
        grid.Add(new EhmrSearchControl(),0);
        grid.Add(new EhmrDatePicker{Title="From"},1);
        grid.Add(new EhmrDatePicker{Title="To"},2);
        Content=grid;
    }
}

public sealed class EhmrKpiControl : ContentView
{
    public static readonly BindableProperty TitleProperty=BindableProperty.Create(nameof(Title),typeof(string),typeof(EhmrKpiControl),"");
    public static readonly BindableProperty ValueProperty=BindableProperty.Create(nameof(Value),typeof(string),typeof(EhmrKpiControl),"");
    public static readonly BindableProperty CaptionProperty=BindableProperty.Create(nameof(Caption),typeof(string),typeof(EhmrKpiControl),"");
    public static readonly BindableProperty AccentProperty=BindableProperty.Create(nameof(Accent),typeof(Color),typeof(EhmrKpiControl),null);
    public string Title { get=>(string)GetValue(TitleProperty);set=>SetValue(TitleProperty,value); }
    public string Value { get=>(string)GetValue(ValueProperty);set=>SetValue(ValueProperty,value); }
    public string Caption { get=>(string)GetValue(CaptionProperty);set=>SetValue(CaptionProperty,value); }
    public Color? Accent { get=>(Color?)GetValue(AccentProperty);set=>SetValue(AccentProperty,value); }
    public EhmrKpiControl()
    {
        var title=new Label{FontSize=11,FontAttributes=FontAttributes.Bold}; title.SetDynamicResource(Label.TextColorProperty,"EhmrTextSecondary"); title.SetBinding(Label.TextProperty,new Binding(nameof(Title),source:this));
        var value=new Label{FontSize=24,FontAttributes=FontAttributes.Bold}; value.SetDynamicResource(Label.TextColorProperty,"EhmrTextPrimary"); value.SetBinding(Label.TextProperty,new Binding(nameof(Value),source:this));
        var caption=new Label{FontSize=11}; caption.SetDynamicResource(Label.TextColorProperty,"EhmrTextMuted"); caption.SetBinding(Label.TextProperty,new Binding(nameof(Caption),source:this));
        var accent=new BoxView{WidthRequest=3,HorizontalOptions=LayoutOptions.Start,VerticalOptions=LayoutOptions.Fill}; accent.SetDynamicResource(BoxView.ColorProperty,"EhmrPrimary");
        Content=new Border{Padding=14,BackgroundColor=Colors.White,Stroke=(Color)Application.Current!.Resources["EhmrBorder"],StrokeThickness=1,StrokeShape=new RoundRectangle{CornerRadius=8},Content=new Grid{ColumnDefinitions=new ColumnDefinitionCollection{new(4),new(GridLength.Star)},ColumnSpacing=12,Children={accent,title,value,caption}}};
        Grid.SetColumn(title,1);Grid.SetColumn(value,1);Grid.SetColumn(caption,1);
    }
}
