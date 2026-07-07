using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Domain.SparkForm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            IAuthorizationService authService,
            ISparkFormBuilder formBuilder)
            : base(
                navigationService,
                dialogService,
                menuService,
                authService,
                formBuilder)
        {
            FormMode=SparkFormMode.Filter;

            BuildForm();
        }

        [RelayCommand]
        protected virtual async Task ApplyAsync()
        {
        }
    }
}
