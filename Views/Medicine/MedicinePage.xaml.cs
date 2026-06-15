using System;
using Microsoft.Maui.Controls;
using EHMR.Domain.Entities;
using EHMR.ViewModels;

namespace EHMR.Views;

public partial class MedicinePage : ContentPage
{
    /// <summary>
    /// private MedicineViewModel _vm = new();
    /// </summary>
   // private readonly MedicineViewModel _vm;
    public MedicinePage()
    {
        InitializeComponent(); //Refresh();
    }

    protected void OnAppearing()
    {
        base.OnAppearing();
        if(BindingContext is DashboardViewModel vm)
        {
            _=vm.Initialize();
        }
    }

    //  private void Refresh() => lst.ItemsSource=_vm.FetchAll();

    //private void OnAdd(object s, EventArgs e)
    //{
    //    _vm.Add(new Medicine { Name=mName.Text, DefaultDosage=mDose.Text }); Refresh();
    //}
}