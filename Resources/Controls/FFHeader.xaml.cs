using System.Windows.Input;


namespace EHMR.Resources.Controls;

public partial class FFPageHeader : ContentView
{
    public FFPageHeader()
    {
        InitializeComponent();
    }

    #region Header Content
    public static readonly BindableProperty PrimaryButtonTextProperty =
    BindableProperty.Create(
        nameof(PrimaryButtonText),
        typeof(string),
        typeof(FFPageHeader));

    public string PrimaryButtonText
    {
        get => (string)GetValue(PrimaryButtonTextProperty);
        set => SetValue(PrimaryButtonTextProperty, value);
    }

    public static readonly BindableProperty PrimaryButtonCommandProperty =
        BindableProperty.Create(
            nameof(PrimaryButtonCommand),
            typeof(ICommand),
            typeof(FFPageHeader));

    public ICommand PrimaryButtonCommand
    {
        get => (ICommand)GetValue(PrimaryButtonCommandProperty);
        set => SetValue(PrimaryButtonCommandProperty, value);
    }

    public static readonly BindableProperty ShowPrimaryButtonProperty =
        BindableProperty.Create(
            nameof(ShowPrimaryButton),
            typeof(bool),
            typeof(FFPageHeader),
            false);

    public bool ShowPrimaryButton
    {
        get => (bool)GetValue(ShowPrimaryButtonProperty);
        set => SetValue(ShowPrimaryButtonProperty, value);
    }

    public static readonly BindableProperty SecondaryButtonTextProperty =
        BindableProperty.Create(
            nameof(SecondaryButtonText),
            typeof(string),
            typeof(FFPageHeader));

    public string SecondaryButtonText
    {
        get => (string)GetValue(SecondaryButtonTextProperty);
        set => SetValue(SecondaryButtonTextProperty, value);
    }

    public static readonly BindableProperty SecondaryButtonCommandProperty =
        BindableProperty.Create(
            nameof(SecondaryButtonCommand),
            typeof(ICommand),
            typeof(FFPageHeader));

    public ICommand SecondaryButtonCommand
    {
        get => (ICommand)GetValue(SecondaryButtonCommandProperty);
        set => SetValue(SecondaryButtonCommandProperty, value);
    }

    public static readonly BindableProperty ShowSecondaryButtonProperty =
        BindableProperty.Create(
            nameof(ShowSecondaryButton),
            typeof(bool),
            typeof(FFPageHeader),
            false);

    public bool ShowSecondaryButton
    {
        get => (bool)GetValue(ShowSecondaryButtonProperty);
        set => SetValue(ShowSecondaryButtonProperty, value);
    }

    public static readonly BindableProperty RefreshCommandProperty =
        BindableProperty.Create(
            nameof(RefreshCommand),
            typeof(ICommand),
            typeof(FFPageHeader));

    public ICommand RefreshCommand
    {
        get => (ICommand)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    public static readonly BindableProperty ShowRefreshButtonProperty =
        BindableProperty.Create(
            nameof(ShowRefreshButton),
            typeof(bool),
            typeof(FFPageHeader),
            false);

    public bool ShowRefreshButton
    {
        get => (bool)GetValue(ShowRefreshButtonProperty);
        set => SetValue(ShowRefreshButtonProperty, value);
    }
    public static readonly BindableProperty HeaderContentProperty =
        BindableProperty.Create(
            nameof(HeaderContent),
            typeof(View),
            typeof(FFPageHeader));

    public View? HeaderContent
    {
        get => (View?)GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    #endregion

    #region Text

    public static readonly BindableProperty CaptionProperty =
        BindableProperty.Create(
            nameof(Caption),
            typeof(string),
            typeof(FFPageHeader),
            string.Empty);

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(FFPageHeader),
            string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(
            nameof(Subtitle),
            typeof(string),
            typeof(FFPageHeader),
            string.Empty);

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    #endregion

    #region Primary Button

    

  

   

    #endregion

    #region Secondary Button

   

    

    #endregion

    #region Refresh Button


    #endregion

    #region Appearance

    public static readonly BindableProperty TitleFontSizeProperty =
        BindableProperty.Create(
            nameof(TitleFontSize),
            typeof(double),
            typeof(FFPageHeader),
            24d);

    public double TitleFontSize
    {
        get => (double)GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }

    public static readonly BindableProperty SubtitleFontSizeProperty =
        BindableProperty.Create(
            nameof(SubtitleFontSize),
            typeof(double),
            typeof(FFPageHeader),
            12d);

    public double SubtitleFontSize
    {
        get => (double)GetValue(SubtitleFontSizeProperty);
        set => SetValue(SubtitleFontSizeProperty, value);
    }

    public static readonly BindableProperty TitleColorProperty =
        BindableProperty.Create(
            nameof(TitleColor),
            typeof(Color),
            typeof(FFPageHeader),
            Color.FromArgb("#111827"));

    public Color TitleColor
    {
        get => (Color)GetValue(TitleColorProperty);
        set => SetValue(TitleColorProperty, value);
    }

    public static readonly BindableProperty SubtitleColorProperty =
        BindableProperty.Create(
            nameof(SubtitleColor),
            typeof(Color),
            typeof(FFPageHeader),
            Color.FromArgb("#6B7280"));

    public Color SubtitleColor
    {
        get => (Color)GetValue(SubtitleColorProperty);
        set => SetValue(SubtitleColorProperty, value);
    }

    public static readonly BindableProperty SubtitleSpacingProperty =
        BindableProperty.Create(
            nameof(SubtitleSpacing),
            typeof(double),
            typeof(FFPageHeader),
            2d);

    public double SubtitleSpacing
    {
        get => (double)GetValue(SubtitleSpacingProperty);
        set => SetValue(SubtitleSpacingProperty, value);
    }

    #endregion
}