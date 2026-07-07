using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Interfaces;
using EHMR.Domain.SparkForm;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            ISparkFormBuilder formBuilder)
            : base(
                navigationService,
                dialogService,
                menuService,
                authService)
        {
            FormBuilder=formBuilder;

            Item=new TEntity();
        }

        [ObservableProperty]
        private TEntity item;

        [ObservableProperty]
        private SparkFormDefinition? formDefinition;

        [ObservableProperty]
        private SparkFormMode formMode;

        protected void BuildForm()
        {
            FormDefinition=
                FormBuilder.Build(
                    Item,
                    FormMode);
        }

        public virtual void RefreshForm()
        {
            BuildForm();
        }

        public virtual bool Validate()
        {
            return true;
        }
    }
}
