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

        private void OnMenuGroupPointerEntered(object? sender, PointerEventArgs e)
        {
            if(sender is Border border)
                border.BackgroundColor=ResolveColor("SparkAccentTeal", "#22B8C6");
        }

        private void OnMenuGroupPointerExited(object? sender, PointerEventArgs e)
        {
            if(sender is not Border border)
                return;

            var isActive=border.BindingContext is not null && (bool)(border.BindingContext.GetType().GetProperty("IsActive")?.GetValue(border.BindingContext) ?? false);
            border.BackgroundColor=isActive
                ? ResolveColor("SidebarActiveBg", "#4DD9C7")
                : Color.FromArgb("#4A5863");
        }

        private void OnMenuItemPointerEntered(object? sender, PointerEventArgs e)
        {
            if(sender is Grid grid)
                grid.BackgroundColor=ResolveColor("SparkAccentTeal", "#22B8C6");
        }

        private void OnMenuItemPointerExited(object? sender, PointerEventArgs e)
        {
            if(sender is not Grid grid)
                return;

            var isActive=grid.BindingContext is not null && (bool)(grid.BindingContext.GetType().GetProperty("IsActive")?.GetValue(grid.BindingContext) ?? false);
            grid.BackgroundColor=isActive
                ? Color.FromArgb("#26323D")
                : Colors.Transparent;
        }

        private static Color ResolveColor(string key, string fallback)
        {
            if(Application.Current?.Resources.TryGetValue(key, out var value)==true && value is Color color)
                return color;

            return Color.FromArgb(fallback);
        }

        // Code-behind
       
    }
}