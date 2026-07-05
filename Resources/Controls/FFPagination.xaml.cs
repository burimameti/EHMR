using System.Windows.Input;

namespace EHMR.Resources.Controls;

public partial class FFPagination : ContentView
{
    public FFPagination()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty SummaryTextProperty =
        BindableProperty.Create(nameof(SummaryText), typeof(string), typeof(FFPagination), string.Empty);

    public static readonly BindableProperty PageInfoProperty =
        BindableProperty.Create(nameof(PageInfo), typeof(string), typeof(FFPagination), string.Empty);

    public static readonly BindableProperty CanPrevProperty =
        BindableProperty.Create(nameof(CanPrev), typeof(bool), typeof(FFPagination), false);

    public static readonly BindableProperty CanNextProperty =
        BindableProperty.Create(nameof(CanNext), typeof(bool), typeof(FFPagination), false);

    public static readonly BindableProperty PrevCommandProperty =
        BindableProperty.Create(nameof(PrevCommand), typeof(ICommand), typeof(FFPagination));

    public static readonly BindableProperty NextCommandProperty =
        BindableProperty.Create(nameof(NextCommand), typeof(ICommand), typeof(FFPagination));

    public string SummaryText
    {
        get => (string)GetValue(SummaryTextProperty);
        set => SetValue(SummaryTextProperty, value);
    }

    public string PageInfo
    {
        get => (string)GetValue(PageInfoProperty);
        set => SetValue(PageInfoProperty, value);
    }

    public bool CanPrev
    {
        get => (bool)GetValue(CanPrevProperty);
        set => SetValue(CanPrevProperty, value);
    }

    public bool CanNext
    {
        get => (bool)GetValue(CanNextProperty);
        set => SetValue(CanNextProperty, value);
    }

    public ICommand PrevCommand
    {
        get => (ICommand)GetValue(PrevCommandProperty);
        set => SetValue(PrevCommandProperty, value);
    }

    public ICommand NextCommand
    {
        get => (ICommand)GetValue(NextCommandProperty);
        set => SetValue(NextCommandProperty, value);
    }
}