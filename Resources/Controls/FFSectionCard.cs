using Microsoft.Maui.Controls.Shapes;

namespace EHMR.Resources.Controls;

/// <summary>
/// Единствена надворешна карта за CRUD секции: бела квадратна површина
/// со teal линија лево. Search/grid контролите не цртаат сопствена карта.
/// </summary>
public sealed class FFSectionCard : Border
{
    private readonly Grid _layout;
    private readonly ContentView _contentHost;
    private bool _wrappingContent;

    public static readonly BindableProperty ContentPaddingProperty =
        BindableProperty.Create(nameof(ContentPadding), typeof(Thickness), typeof(FFSectionCard),
            new Thickness(18), propertyChanged: (bindable, _, value) =>
                SetContentPadding((FFSectionCard)bindable, (Thickness)value));

    private static void SetContentPadding(FFSectionCard card, Thickness padding)
    {
        if(card._contentHost is not null)
            card._contentHost.Padding=padding;
    }
    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public FFSectionCard()
    {
        Padding=0;
        Margin=new Thickness(16, 0, 16, 0);
        StrokeThickness=1;
        StrokeShape=new RoundRectangle { CornerRadius=0 };
        Shadow=null;
        SetDynamicResource(BackgroundColorProperty, "White");
        SetDynamicResource(StrokeProperty, "BorderColor");

        var rail=new BoxView { WidthRequest=4, HorizontalOptions=LayoutOptions.Fill };
        rail.SetDynamicResource(BoxView.ColorProperty, "SidebarActiveBg");

        _contentHost=new ContentView { Padding=ContentPadding };
        _layout=new Grid
        {
            ColumnDefinitions=
            {
                new ColumnDefinition(new GridLength(4)),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing=0
        };
        _layout.Add(rail, 0);
        _layout.Add(_contentHost, 1);

        _wrappingContent=true;
        Content=_layout;
        _wrappingContent=false;
    }

    protected override void OnPropertyChanged(string? propertyName=null)
    {
        base.OnPropertyChanged(propertyName);
        if(_contentHost is null||_layout is null||_wrappingContent||propertyName!=ContentProperty.PropertyName||Content is not View content||ReferenceEquals(content, _layout))
            return;

        _wrappingContent=true;
        _contentHost.Content=content;
        Content=_layout;
        _wrappingContent=false;
    }
}