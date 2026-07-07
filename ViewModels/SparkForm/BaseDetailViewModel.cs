using EHMR.Domain.Interfaces;
using EHMR.Domain.SparkForm;


namespace EHMR.ViewModels.SparkForm
{
    public abstract partial class BaseDetailViewModel<TEntity>
     : BaseFormViewModel<TEntity>
     where TEntity : class, new()
    {
        protected BaseDetailViewModel(
            INavigationService navigationService,
            IUserDialogService dialogService,
            IMenuService menuService,
            IAuthorizationService authService,
            ISparkFormBuilder formBuilder)
            : base(
                navigationService,
                dialogService,
                menuService,
                authService,
                formBuilder)
        {
            FormMode=SparkFormMode.Detail;
        }

        public virtual async Task LoadAsync(TEntity entity)
        {
            Item=entity;

            BuildForm();

            await Task.CompletedTask;
        }
    }
}
