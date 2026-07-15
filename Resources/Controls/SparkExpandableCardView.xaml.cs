namespace EHMR.Resources.Controls;

public enum SparkCardVariant
{
    Default,
    Patient,
    Encounter,
    Therapy,
    Diagnosis,
    Document,
    Medicine,
    Prescription,
    Appointment,
    Warning,
    Success,
    TherapyCycle
}

public partial class SparkExpandableCard : ContentView
{
    public SparkExpandableCard()
    {
        InitializeComponent();

        Loaded+=(_, _) =>
        {
            ApplyContent();
            ApplyVariant(Variant);
            UpdateExpandState(IsExpanded);
        };
    }

    #region Bindable Properties

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(SparkExpandableCard),
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
            typeof(SparkExpandableCard),
            string.Empty);

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(
            nameof(Icon),
            typeof(string),
            typeof(SparkExpandableCard),
            string.Empty);

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly BindableProperty IsExpandedProperty =
        BindableProperty.Create(
            nameof(IsExpanded),
            typeof(bool),
            typeof(SparkExpandableCard),
            false,
            propertyChanged: OnExpandedChanged);

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    private static void OnExpandedChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is SparkExpandableCard card)
        {
            card.UpdateExpandState((bool)newValue);
        }
    }

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(
            nameof(Variant),
            typeof(SparkCardVariant),
            typeof(SparkExpandableCard),
            SparkCardVariant.Default,
            propertyChanged: OnVariantChanged);

    public SparkCardVariant Variant
    {
        get => (SparkCardVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    private static void OnVariantChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is SparkExpandableCard card)
        {
            card.ApplyVariant((SparkCardVariant)newValue);
        }
    }

    public static readonly BindableProperty CardContentProperty =
        BindableProperty.Create(
            nameof(CardContent),
            typeof(View),
            typeof(SparkExpandableCard),
            null,
            propertyChanged: OnCardContentChanged);

    public View? CardContent
    {
        get => (View?)GetValue(CardContentProperty);
        set => SetValue(CardContentProperty, value);
    }

    private static void OnCardContentChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is SparkExpandableCard card)
        {
            card.ApplyContent();
        }
    }

    #endregion

    #region Content

    private void ApplyContent()
    {
        if(ContentHost==null)
            return;

        ContentHost.Content=CardContent;
    }

    #endregion

    #region Expand

    private void ExpandTapped(
        object sender,
        TappedEventArgs e)
    {
        IsExpanded=!IsExpanded;
    }

    private void UpdateExpandState(bool expanded)
    {
        if(ExpandableArea==null)
            return;

        ExpandableArea.IsVisible=expanded;
        ExpandableArea.Opacity=expanded ? 1 : 0;

        if(ExpandIcon!=null)
        {
            ExpandIcon.Text=
                expanded
                ? "▴"
                : "▾";
        }
    }

    #endregion

    #region Variant Icon

    // НАПОМЕНА: Сите варијанти сега го делат истиот Spark teal акцент
    // (SparkTabActiveBg / SparkAccentTealBg / SparkAccentTealDark), веќе
    // поставени директно во XAML. Ова е единственото место што варира по
    // Variant — само иконата се менува, боите остануваат исти за кохезија
    // со остатокот на формата и за да не исчезнува текстот.
    private void ApplyVariant(SparkCardVariant variant)
    {
        Icon=variant switch
        {
            SparkCardVariant.Patient => "👤",
            SparkCardVariant.Diagnosis => "🩺",
            SparkCardVariant.Encounter => "📋",
            SparkCardVariant.Appointment => "📅",
            SparkCardVariant.TherapyCycle => "🔄",
            SparkCardVariant.Therapy => "🔄",
            SparkCardVariant.Prescription => "💊",
            SparkCardVariant.Medicine => "💉",
            SparkCardVariant.Document => "📄",
            SparkCardVariant.Warning => "⚠️",
            SparkCardVariant.Success => "✅",
            _ => "•",
        };
    }

    #endregion
}