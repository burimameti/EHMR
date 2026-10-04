using EHMR.Services;
using EHMR.ViewModels;
using Microsoft.Maui.Controls;

namespace EHMR.Views
{
    public partial class MenuView : ContentView
    {
        private readonly MenuViewModel _vm;

        public MenuView(MenuViewModel vm)
        {
            InitializeComponent();
            _vm=vm;
            BindingContext=_vm;
        }

        private void OnItemSelected(object sender, SelectionChangedEventArgs e)
        {
            //if(e.CurrentSelection.FirstOrDefault() is NavigationItem item)
            //{
            //    if(BindingContext is MenuViewModel vm)
            //        _=vm.Navigate(item);
            //}

            //((CollectionView)sender).SelectedItem=null;
        }

        private void OnGroupPointerEntered(object sender, PointerEventArgs e)
        {
            if(sender is BindableObject view && view.BindingContext is EHMR.Constants.NavigationGroup group)
                group.IsHovered=true;
        }

        private void OnGroupPointerExited(object sender, PointerEventArgs e)
        {
            if(sender is BindableObject view && view.BindingContext is EHMR.Constants.NavigationGroup group)
                group.IsHovered=false;
        }

        private void OnChildPointerEntered(object sender, PointerEventArgs e)
        {
            if(sender is BindableObject view && view.BindingContext is EHMR.Constants.NavigationItem item)
                item.IsHovered=true;
        }

        private void OnChildPointerExited(object sender, PointerEventArgs e)
        {
            if(sender is BindableObject view && view.BindingContext is EHMR.Constants.NavigationItem item)
                item.IsHovered=false;
        }
    }
}