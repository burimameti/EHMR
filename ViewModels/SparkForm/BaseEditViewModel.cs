using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Domain.SparkForm;

namespace EHMR.ViewModels.SparkForm
{
    public interface ISparkEntityService<TEntity> where TEntity : class, new()
    {
        Task<TEntity> GetAsync(Guid id);
        Task SaveAsync(TEntity entity);
    }

    public abstract partial class BaseEditDetailViewModel<TEntity>
        : BaseFormViewModel<TEntity>
        where TEntity : class, new()
    {
        private readonly ISparkEntityService<TEntity> _entityService;
        private readonly ISelectedItemService<TEntity> _selectedItemService;

        private TEntity? _original;
        private bool _isNew;

        protected BaseEditDetailViewModel(
            INavigationService navigationService,
            IUserDialogService dialogService,
            IMenuService menuService,
            IAuthorizationService authService,
            ISparkFormBuilder formBuilder,
            ISparkEntityService<TEntity> entityService,
            ISelectedItemService<TEntity> selectedItemService)
            : base(navigationService, dialogService, menuService, authService,selectedItemService, formBuilder)
        {
            _entityService=entityService;
            _selectedItemService=selectedItemService;
            FormMode=SparkFormMode.Detail;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        private bool isReadOnly = true;

        public bool IsEditMode => !IsReadOnly;

        public virtual async Task LoadAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                var selected = _selectedItemService.SelectedItem;
                _isNew=selected==null;

                Item=selected??new TEntity();
                _original=_isNew ? null : Clone(Item);

                FormMode=_isNew ? SparkFormMode.Create : SparkFormMode.Detail;
                IsReadOnly=!_isNew;

                BuildForm();
            }, "Грешка при вчитување");
        }

        [RelayCommand]
        private void ToggleEdit()
        {
            if(!CanUpdate) return;

            FormMode=SparkFormMode.Edit;
            IsReadOnly=false;
            BuildForm();
        }

        [RelayCommand]
        private async Task Cancel()
        {
            var confirmed = await UserDialogService.ShowConfirmationAsync(
                "Откажи",
                "Дали сте сигурни дека сакате да се вратите назад без да ги зачувате промените?",
                "Да",
                "Не");
            if(!confirmed) return;

            if(_isNew)
            {
                NavigateBack();
                return;
            }

            if(_original!=null)
                Item=Clone(_original);

            FormMode=SparkFormMode.Detail;
            IsReadOnly=true;
            BuildForm();
        }

        [RelayCommand]
        private async Task Save()
        {
            if(!Validate())
                return;

            if(_isNew&&!CanCreate) return;
            if(!_isNew&&!CanUpdate) return;

            var confirmed = await UserDialogService.ShowConfirmationAsync(
                "Зачувај",
                "Дали сте сигурни дека сакате да ги зачувате податоците?",
                "Да",
                "Не");
            if(!confirmed) return;

            await ExecuteSafeAsync(async () =>
            {
                await _entityService.SaveAsync(Item);

                if(_isNew)
                {
                    NavigateBack();
                    return;
                }

                _original=Clone(Item);
                FormMode=SparkFormMode.Detail;
                IsReadOnly=true;
                BuildForm();
            }, "Грешка при зачувување");
        }

        protected virtual TEntity Clone(TEntity source) => source; // override per entity (e.g. Appointment.Clone())
        protected virtual void NavigateBack() => _=NavigationService.GoToAsync("..");
    }
}