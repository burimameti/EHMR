using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Domain.SparkForm;
using EHMR.Services;
using EHMR.ViewModels.SparkForm;
using Microsoft.UI.Xaml.Controls;

public abstract partial class BaseEditViewModel<TEntity>
    : BaseFormViewModel<TEntity>
    where TEntity : class, new()
{
    protected BaseEditViewModel(
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
        FormMode=SparkFormMode.Edit;
    }

    public virtual async Task LoadAsync(TEntity entity)
    {
        Item=entity;

        BuildForm();

        await Task.CompletedTask;
    }

    [RelayCommand]
    protected virtual async Task SaveAsync()
    {
    }

    [RelayCommand]
    protected virtual async Task DeleteAsync()
    {
    }

    [RelayCommand]
    protected virtual async Task CancelAsync()
    {
        await NavigationService.GoBackAsync();
    }
}