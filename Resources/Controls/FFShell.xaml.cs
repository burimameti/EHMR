using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.Resources.Controls;

public partial class FFShell : ContentView
{
    public FFShell()
    {
        InitializeComponent();
    }

    // ================= TITLE =================
    public static readonly BindableProperty AppTitleProperty =
        BindableProperty.Create(nameof(AppTitle), typeof(string), typeof(FFShell), "EHMR");

    public string AppTitle
    {
        get => (string)GetValue(AppTitleProperty);
        set => SetValue(AppTitleProperty, value);
    }

    // ================= USER =================
    public static readonly BindableProperty UserNameProperty =
        BindableProperty.Create(nameof(UserName), typeof(string), typeof(FFShell));

    public string UserName
    {
        get => (string)GetValue(UserNameProperty);
        set => SetValue(UserNameProperty, value);
    }

    // ================= CONTENT HOST =================
    public static readonly BindableProperty ContentViewProperty =
        BindableProperty.Create(nameof(ContentView), typeof(View), typeof(FFShell));

    public View ContentView
    {
        get => (View)GetValue(ContentViewProperty);
        set => SetValue(ContentViewProperty, value);
    }

    // ================= MENU =================
    public static readonly BindableProperty MenuItemsProperty =
        BindableProperty.Create(nameof(MenuItems),
            typeof(ObservableCollection<MenuItem>),
            typeof(FFShell));

    public ObservableCollection<MenuItem> MenuItems
    {
        get => (ObservableCollection<MenuItem>)GetValue(MenuItemsProperty);
        set => SetValue(MenuItemsProperty, value);
    }
}