using EHMR.Domain.Entities;
using EHMR.ViewModels;



namespace EHMR.Views.Doctors;

public partial class DoctorsListPage : ContentPage
{
    private readonly DoctorsListViewModel _viewModel;


    public DoctorsListPage(
        DoctorsListViewModel viewModel,
        MenuView menu)
    {
        InitializeComponent();


        _viewModel=viewModel;

        BindingContext=_viewModel;


        MenuHost.Content=menu;
    }



    protected override async void OnAppearing()
    {
        base.OnAppearing();


        if(BindingContext is DoctorsListViewModel vm)
            await vm.LoadAsync();
    }




    private async void OnActionMenuTapped(
        object sender,
        TappedEventArgs e)
    {
        var doctor = e.Parameter as Doctor;

        if(doctor==null)
            return;



        var action = await DisplayActionSheet(
            $"{doctor.FullName}",
            "Откажи",
            null,
            "👁 Преглед",
            "✎ Уреди");



        switch(action)
        {
            case "👁 Преглед":

                await _viewModel.SelectCommand
                    .ExecuteAsync(doctor);

                break;



            case "✎ Уреди":

                await _viewModel.EditCommand
                    .ExecuteAsync(doctor);

                break;
        }
    }
}