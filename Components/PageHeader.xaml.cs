namespace EHMR.Components
{
    public partial class PageHeader : Grid
    {
        public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(PageHeader));

        public string Title
        {
            get => (string)GetValue(TitleProperty);

            set => SetValue(TitleProperty, value);
        }

        public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(
        nameof(Subtitle),
        typeof(string),
        typeof(PageHeader));

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);

            set => SetValue(SubtitleProperty, value);
        }

        public PageHeader()
        {
            InitializeComponent();
            BindingContext=this;
        }
    }
}