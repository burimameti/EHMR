
using EHMR.ViewModels.Encounters;

namespace EHMR.Views.Encounters
{
    public partial class EncounterListPage : ContentPage
    {
        private readonly MenuView menuView;

        public EncounterListPage(EncounterListViewModel viewModel, MenuView menuView)
        {
            InitializeComponent();

            // Доделување на ViewModel како контекст за податоци
            BindingContext=viewModel;
            this.menuView=menuView;
            MenuHost.Content=this.menuView;
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if(BindingContext is EncounterListViewModel vm)
            {
                await vm.LoadAsync();
            }
        }
        //private void OnSelectedStatusChanged(object sender, EventArgs e)
        //{
        //    if(BindingContext is not EncounterListViewModel vm)
        //        return;

        //    vm.StatusChangedCommand.Execute(((FFPicker)sender).SelectedItem?.ToString());
        //}

        //private void OnSelectedPriorityChanged(object sender, EventArgs e)
        //{
        //    if(BindingContext is not EncounterListViewModel vm)
        //        return;

        //    vm.PriorityFilters.Execute(((FFPicker)sender).SelectedItem?.ToString());
        //}

        //private void OnSelectedEncounterTypeChanged(object sender, EventArgs e)
        //{
        //    if(BindingContext is not EncounterListViewModel vm)
        //        return;

        //    vm.EncounterTypeChangedCommand.Execute(((FFPicker)sender).SelectedItem?.ToString());
        //}
        //private void OnSelectedStatusChanged(object sender, EventArgs e)
        //{
        //    if(BindingContext is not EncounterListViewModel vm)
        //        return;

        //    var picker = (Picker)sender;

        //    var display = picker.SelectedItem?.ToString();

        //    vm.SelectedStatus=string.IsNullOrWhiteSpace(display)
        //        ? "All"
        //        : EncounterStatusSchema.ToKeyFromDisplay(display);

        //    vm.CurrentPage=1;
        //    vm.ApplyFiltersAndRefreshStats();
        //}


    }
}
