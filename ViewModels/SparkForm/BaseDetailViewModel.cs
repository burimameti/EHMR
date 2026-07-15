using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Interfaces;

namespace EHMR.ViewModels
{
    // Shared by BOTH hand-authored detail pages (like Appointment) and
    // SparkForm-reflection-driven ones. No FormBuilder dependency here.
    public abstract partial class BaseDetailViewModel<T> : BaseViewModel<T>
        where T : class, new()
    {
        protected BaseDetailViewModel(
            INavigationService navigationService,
            IUserDialogService dialogService,
            IMenuService menuService,
            IAuthorizationService authService,
            ISelectedItemService<T> selectedItemService)
            : base(navigationService, dialogService, menuService, authService, selectedItemService)
        {
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        private bool isReadOnly = true;

        public bool IsEditMode => !IsReadOnly;

        protected sealed override IEnumerable<T> ApplyFilters(IEnumerable<T> query) => query;

        protected sealed override void ResetFilters()
        {
        }

        protected virtual void ToggleEdit()
        {
            if(!CanUpdate) return;
            IsReadOnly=false;
        }
    }
}