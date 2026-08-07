using EHMR.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Collections.Generic;

namespace EHMR.Views
{
    public partial class RootLayoutPage : ContentView
    {
        private readonly MenuViewModel _menuViewModel;
        private readonly Stack<View> _navigationStack = new();

        public RootLayoutPage(MenuViewModel menuViewModel)
        {
            InitializeComponent();
            _menuViewModel=menuViewModel;
            MenuHost.Content=new MenuView(_menuViewModel)
            {
                BindingContext=menuViewModel
            };
        }

        public void NavigateTo(View page)
        {
            PageHost.Content=page;
        }

        // =========================
        // BACK NAVIGATION
        // =========================
        public void GoBack()
        {
            if(_navigationStack.Count<=1)
                return;

            _navigationStack.Pop();

            var previous = _navigationStack.Peek();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PageHost.Content=previous;
            });
        }

        // =========================
        // SET ROOT CONTENT
        // =========================
        public void SetPage(View page)
        {
            _navigationStack.Push(page);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PageHost.Content=page;
            });
        }
    }
}