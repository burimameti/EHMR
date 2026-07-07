namespace EHMR.Domain.SparkForm
{
    public enum SparkFieldType
    {
        Auto,
        Text,
        Multiline,
        Password,
        Email,
        Phone,
        Url, 
        Integer,
        Long,
        Decimal,
        Double,
        Currency,
        Percentage,
        Date,
        Time,
        DateTime,
        Boolean,
        Toggle,
        CheckBox,
        Radio,
        Combo,
        Lookup,
        Search,
        AutoComplete,
        Token,
        Color,
        Image,
        Avatar,
        File,
        Attachment,
        RichText,
        Html,
        Markdown,
        Barcode,
        QrCode,
        Signature,
        Slider,
        Rating,
        Progress,
        Label,
        Separator,
        Custom,
        ReadOnly,
        Badge
    }
    public enum SparkFieldLayout
    {
        Horizontal,
        Vertical,
        Inline,
        Stacked
    }
    public enum SparkValidationType
    {
        None,
        Required,
        Email,
        Phone,   Length,
        MinLength,
        MaxLength,  Range,
        Regex,
        Url,
        Integer,
        Long,
        Decimal,
        Double,
        Currency,
        Percentage,
        Date,
        Time,
        DateTime,   Compare,
        CreditCard,
        Custom,
        Async
    } 
    public enum SparkFieldVisibility
    {
        Visible,
        Hidden,
        Collapsed
    }
    public enum SparkFieldReadOnly
    {
        Editable,
        ReadOnly,
        Disabled
    } 
    public enum SparkFieldSection
    {
        Header,
        Body,
        Footer,
        Sidebar,
        Custom
    }
    public enum SparkSectionLayout
    {
        Horizontal,
        Vertical,
        Inline,
        Stacked,   Grid,
        Card,
        GroupBox,
        Accordion,
        Tab,Stack,
        Expander
    }
    public enum SparkButtonStyle
    {
        Primary,
        Secondary,
        Tertiary,
        Danger,
        Link,
        Custom
    }
    public enum SparkButtonSize
    {
        Small,
        Medium,
        Large,
        ExtraLarge,
        Custom
    }
    public enum SparkColumnWidth
    {
      Auto,
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Fill
    }
    public enum SparkButtonPosition
    {
       Top,
        Bottom,
        Left,
        Right,
        Floating
    }
    public enum SparkAlignment
    {
        Start,
        Center,
        End,
        Stretch
    }
    public enum SparkFieldState
    {
         Normal,
        Focused,
        Disabled,
        ReadOnly,
        Required,
        Error,
        Valid,
        Hidden
    }
 
    public enum SparkLookupMode
    {
        Popup,
        Dialog,
        Inline,
        AutoComplete,
        Search
    }
    public enum SparkToolbarButton
    { 
        Back,
        Refresh,
        Save,
        SaveAndNew,
        Delete,
        Print,
        Export,
        History,
        Audit,
        Duplicate,
        Close,
        More
    }
    public enum SparkFooterButton
    { Save,
        Cancel,
        Delete,
        Close,
        Apply,
        Reset,
        Previous,
        Next,
        Finish
    }
    public enum SparkFormStyle
    {   Default,
        Outlined,
        Filled,
        Material,
        Fluent,
        Spark,
        Magento,
        Minimal
    }

}