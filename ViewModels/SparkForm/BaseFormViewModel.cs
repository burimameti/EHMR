using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Interfaces;
using EHMR.Domain.SparkForm;

namespace EHMR.ViewModels.SparkForm
{
    public abstract partial class BaseFormViewModel<TEntity>
        : BaseViewModel<TEntity>
        where TEntity : class, new()
    {
        protected readonly ISparkFormBuilder FormBuilder;

        protected BaseFormViewModel(
            INavigationService navigationService,
            IUserDialogService dialogService,
            IMenuService menuService,
            IAuthorizationService authService,
            ISelectedItemService<TEntity> selectedItemService,
            ISparkFormBuilder formBuilder)
            : base(navigationService, dialogService, menuService, authService, selectedItemService)
        {
            FormBuilder=formBuilder;
            Item=new TEntity();
            EvaluatePermissions(); // uses ModuleName from the concrete leaf VM
        }

        [ObservableProperty] private TEntity item;
        [ObservableProperty] private SparkFormDefinition? formDefinition;
        [ObservableProperty] private SparkFormMode formMode;
        private IUserDialogService dialogService;

        protected void BuildForm()
        {
            FormDefinition=FormBuilder.Build(Item, FormMode);
        }

        // pull edited field values back onto Item before save/validate
        protected void SyncFormToEntity()
        {
            FormDefinition?.ApplyTo(Item!);
        }

        public virtual bool Validate()
        {
            if(FormDefinition==null) return true;
            SyncFormToEntity();
            return SparkFormValidator.Validate(FormDefinition);
        }

        public virtual void RefreshForm() => BuildForm();

        // BaseViewModel<T>'s list pipeline (search/filter/page) doesn't apply
        // to a single-entity form. Neutralize it here, once, instead of every
        // leaf VM having to stub these out.
        protected sealed override IEnumerable<TEntity> ApplyFilters(IEnumerable<TEntity> query)
            => query;

        protected sealed override void ResetFilters()
        {
        }
    }
}