using Microsoft.Maui.Controls;
using EHMR.ViewModels;

namespace EHMR.Views;

public partial class ReportPage : ContentPage
{
    private readonly ReportViewModel _vm;
    private readonly MenuView _menu;

    public ReportPage(ReportViewModel vm, MenuView menu)
    {
        InitializeComponent();
        _vm=vm;
        _menu=menu;
        BindingContext=_vm;
        MenuHost.Content=_menu;
    }
}