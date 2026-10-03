using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Domain.Entities.Rbac;
using EHMR.Services;
using EHMR.ViewModels;
using System.Collections.ObjectModel;
namespace EHMR.ViewModels.Mkb10;
public partial class Mkb10CodeDetailViewModel : ObservableObject
{
    private readonly IMkb10CodeService _service;
    private readonly ISelectedItemService<Mkb10Code> _selected;
    private readonly IAuthorizationService _authorization;

    [ObservableProperty]
    private Mkb10Code mkb10Code = new();

    [ObservableProperty]
    private bool isEditMode;

    public bool IsReadOnly => !IsEditMode;

    public bool CanCreate => _authorization.CanPerform(Modules.MKBCodes, ModuleAction.Create);
    public bool CanUpdate => _authorization.CanPerform(Modules.MKBCodes, ModuleAction.Edit);
    public bool CanDelete => _authorization.CanPerform(Modules.MKBCodes, ModuleAction.Delete);
    public bool CanEditForm => _isNewMode ? CanCreate : CanUpdate;

    public string HeaderTitle =>
        Mkb10Code.Id== new Guid()
            ? "Нов МКБ-10 Код"
            : Mkb10Code.Code;

    public string HeaderSubtitle =>
        Mkb10Code.Id== new Guid()
            ? "Креирање на нов МКБ-10 код"
            : Mkb10Code.Description;

    public ObservableCollection<string> ChapterOptions { get; } = new();

    public IRelayCommand SaveCommand
    {
        get;
    }

    public IRelayCommand CancelCommand
    {
        get;
    }

    public IRelayCommand ToggleEditModeCommand
    {
        get;
    }

    public Mkb10CodeDetailViewModel(
        IMkb10CodeService service,
        ISelectedItemService<Mkb10Code> selected,
        INavigationService navigation,
        IAuthorizationService authorization)
    {
        _service=service;
        _selected=selected;
        _authorization=authorization;

        SaveCommand=new RelayCommand(async () => await Save());
        CancelCommand=new RelayCommand(async () => await navigation.GoBackAsync());
        ToggleEditModeCommand=new RelayCommand(() =>
        {
            if(!CanUpdate) return;
            IsEditMode=true;
            OnPropertyChanged(nameof(IsReadOnly));
            OnPropertyChanged(nameof(CanEditForm));
        });
    }

    public async Task LoadAsync()
    {
        if(_selected.SelectedItem!=null)
        {
            Mkb10Code=_selected.SelectedItem;
            IsEditMode=false;
        }
        else
        {
            Mkb10Code=new Mkb10Code
            {
                IsActive=true
            };

            IsEditMode=true;
        }

        ChapterOptions.Clear();

        var chapters = (await _service.GetAllAsync())
            .Select(x => x.Chapter)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x);

        foreach(var c in chapters)
            ChapterOptions.Add(c!);

        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(CanEditForm));
    }

    private async Task Save()
    {
        if(!(_isNewMode ? CanCreate : CanUpdate)) return;
        await _service.SaveAsync(Mkb10Code);      

        await Shell.Current.GoToAsync("..");
    }
}