using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Domain.SparkForm;


namespace EHMR.ViewModels.SparkForm
{
    public abstract partial class BaseFilterViewModel<TEntity>
     : BaseFormViewModel<TEntity>
     where TEntity : class, new()
    {
        protected BaseFilterViewModel(
            INavigationService navigationService,
            IUserDialogService dialogService,
            IMenuService menuService,
            IAuthorizationService authService,ISelectedItemService<TEntity> selectedItemService,
            ISparkFormBuilder formBuilder)
            : base(
                navigationService,
                dialogService,
                menuService,
                authService, selectedItemService,
                formBuilder)
        {
            FormMode=SparkFormMode.Filter;

            BuildForm();
        }

    }
}
