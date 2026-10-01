using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Extensions;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.ViewModels.Alerts;

public partial class AlertsListViewModel : ObservableObject, IQueryAttributable
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Patient> _selectedPatient;
    private readonly INavigationService _navigationService;

    private List<AlertGridItem> _allItems = new();

    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string selectedLevel = "Сите";
    [ObservableProperty] private string selectedStatus = "Сите";
    [ObservableProperty] private string pageInfoText = string.Empty;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private int totalItems;

    private const int PageSize = 15;
    private string? _pendingLevel;

    public ObservableCollection<string> LevelFilters { get; } =
        new() { "Сите", "Критични", "Предупредувања", "Информативни" };

    public ObservableCollection<string> StatusFilters { get; } =
        new() { "Сите", "Активни", "Решени" };

    public ICommand SearchCommand { get; }
    public ObservableCollection<SparkPickerItem> Pickers { get; } = new();

    public AlertsListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ISelectedItemService<Patient> selectedPatient,
        INavigationService navigationService)
    {
        _dbFactory=dbFactory;
        _selectedPatient=selectedPatient;
        _navigationService=navigationService;

        SearchCommand=new Command<string>(value =>
        {
            SearchText=value ?? string.Empty;
            ApplyFilter();
        });

        BuildPickers();
        BuildGridColumns();
    }

    private void BuildPickers()
    {
        Pickers.Clear();

        var levelPicker=new SparkPickerItem { Placeholder="Ниво" };
        foreach(var item in LevelFilters) levelPicker.Items.Add(item);
        levelPicker.SelectedItem=SelectedLevel;
        levelPicker.PropertyChanged+=(_, e) =>
        {
            if(e.PropertyName==nameof(SparkPickerItem.SelectedItem) &&
               levelPicker.SelectedItem is string value)
            {
                SelectedLevel=value;
                CurrentPage=1;
                ApplyFilter();
            }
        };

        var statusPicker=new SparkPickerItem { Placeholder="Статус" };
        foreach(var item in StatusFilters) statusPicker.Items.Add(item);
        statusPicker.SelectedItem=SelectedStatus;
        statusPicker.PropertyChanged+=(_, e) =>
        {
            if(e.PropertyName==nameof(SparkPickerItem.SelectedItem) &&
               statusPicker.SelectedItem is string value)
            {
                SelectedStatus=value;
                CurrentPage=1;
                ApplyFilter();
            }
        };

        Pickers.Add(levelPicker);
        Pickers.Add(statusPicker);
    }

    partial void OnSelectedLevelChanged(string value)
    {
        var picker=Pickers.FirstOrDefault();
        if(picker is not null) picker.SelectedItem=value;
    }

    partial void OnSelectedStatusChanged(string value)
    {
        var picker=Pickers.Skip(1).FirstOrDefault();
        if(picker is not null) picker.SelectedItem=value;
    }

    [RelayCommand(AllowConcurrentExecutions=false)]
    public async Task LoadAsync()
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;

            await using var db=await _dbFactory.CreateDbContextAsync();

            var rows=await (
                from a in db.Alerts.AsNoTracking()
                join p in db.Patients.AsNoTracking() on a.PatientId equals p.Id
                orderby a.CreatedAt descending
                select new AlertGridItem
                {
                    Id=a.Id,
                    PatientId=a.PatientId,
                    PatientName=(p.FirstName+" "+p.LastName).Trim(),
                    Message=a.Message,
                    Level=a.Level,
                    IsRead=a.IsRead,
                    IsResolved=a.IsResolved,
                    CreatedAt=a.CreatedAt,
                    DedupKey=a.DedupKey
                }).ToListAsync();

            _allItems=rows;
            ApplyPendingFilter();
        }
        finally
        {
            IsBusy=false;
        }
    }

    private void ApplyPendingFilter()
    {
        if(!string.IsNullOrWhiteSpace(_pendingLevel))
        {
            var parsed=_pendingLevel.ToAlertLevel();
            if(parsed.HasValue)
                SelectedLevel=parsed.Value.ToLabel();
        }

        ApplyFilter();
        _pendingLevel=null;
    }

    private void ApplyFilter()
    {
        IEnumerable<AlertGridItem> query=_allItems;

        if(SelectedLevel!="Сите")
            query=query.Where(x => x.Level.ToLabel()==SelectedLevel);

        if(SelectedStatus=="Активни")
            query=query.Where(x => !x.IsResolved);
        else if(SelectedStatus=="Решени")
            query=query.Where(x => x.IsResolved);

        if(!string.IsNullOrWhiteSpace(SearchText))
        {
            var s=SearchText.Trim();
            query=query.Where(x =>
                x.PatientName.Contains(s,StringComparison.OrdinalIgnoreCase) ||
                x.Message.Contains(s,StringComparison.OrdinalIgnoreCase) ||
                x.DedupKey.Contains(s,StringComparison.OrdinalIgnoreCase));
        }

        var list=query.OrderByDescending(x => x.CreatedAt).ToList();
        TotalItems=list.Count;
        TotalPages=Math.Max(1,(int)Math.Ceiling(list.Count/(double)PageSize));

        if(CurrentPage>TotalPages) CurrentPage=TotalPages;
        if(CurrentPage<1) CurrentPage=1;

        var page=list.Skip((CurrentPage-1)*PageSize).Take(PageSize).ToList();
        GridRows=new ObservableCollection<SparkGridRow>(page.Select(ToGridRow));
        PageInfoText=$"{TotalItems} резултати · страница {CurrentPage} од {TotalPages}";
        OnPropertyChanged(nameof(PageInfoText));
    }

    private SparkGridRow ToGridRow(AlertGridItem item)
    {
        var row=new SparkGridRow { Tag=item };

        row["Ниво"]=new SparkBadgeValue(item.Level.ToLabel(), item.Level switch        {           AlertLevel.Critical => SparkBadgeTone.Danger,         AlertLevel.Warning => SparkBadgeTone.Warning,            _ => SparkBadgeTone.Neutral       });
        row["Пациент"]=item.PatientName;
        row["Опис"]=item.Message;
        row["Датум"]=item.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        row["Статус"]=new SparkBadgeValue(
            item.IsResolved ? "Решено" : "Активно",
            item.IsResolved ? SparkBadgeTone.Success : SparkBadgeTone.Warning);
        row["Прочитано"]=item.IsRead ? "Да" : "Не";

        row["Опции"]=new SparkButtonItem
        {
            IconGlyph="\uf06e",
            Label="Пациент",
            IsPrimary=false,
            Command=OpenPatientCommand,
            CommandParameter=item.PatientId
        };

        return row;
    }

    private void BuildGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header="НИВО", Key="Ниво", CellType=SparkGridCellType.Badge, Width=new GridLength(1.15,GridUnitType.Star) },
            new() { Header="ПАЦИЕНТ", Key="Пациент", Width=new GridLength(1.8,GridUnitType.Star) },
            new() { Header="ОПИС / ПРЕДУПРЕДУВАЊЕ", Key="Опис", Width=new GridLength(3.4,GridUnitType.Star) },
            new() { Header="ДАТУМ", Key="Датум", Width=new GridLength(1.45,GridUnitType.Star) },
            new() { Header="СТАТУС", Key="Статус", CellType=SparkGridCellType.Badge, Width=new GridLength(1.15,GridUnitType.Star) },
            new() { Header="ПРОЧИТАНО", Key="Прочитано", Width=new GridLength(1.0,GridUnitType.Star) },
            new() { Header="ОПЦИИ", Key="Опции", CellType=SparkGridCellType.Button, Width=GridLength.Auto }
        };
    }

    [RelayCommand]
    private async Task OpenPatient(Guid patientId)
    {
        if(patientId==Guid.Empty) return;

        _selectedPatient.SelectedItem=await LoadPatientAsync(patientId);
        if(_selectedPatient.SelectedItem is null) return;

        _selectedPatient.OpenInEditMode=false;
        await _navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    private async Task<Patient?> LoadPatientAsync(Guid patientId)
    {
        await using var db=await _dbFactory.CreateDbContextAsync();
        return await db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.Id==patientId);
    }

    [RelayCommand]
    private void NextPage()
    {
        if(CurrentPage>=TotalPages) return;
        CurrentPage++;
        ApplyFilter();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if(CurrentPage<=1) return;
        CurrentPage--;
        ApplyFilter();
    }

    [RelayCommand]
    private void PageChanged(int page)
    {
        if(page<1 || page>TotalPages) return;
        CurrentPage=page;
        ApplyFilter();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText=string.Empty;
        SelectedLevel="Сите";
        SelectedStatus="Сите";
        CurrentPage=1;
        ApplyFilter();
    }

    public void ApplyQueryAttributes(IDictionary<string,object> query)
    {
        _pendingLevel=query.TryGetValue("level",out var value) ? value?.ToString() : null;
    }
}

public sealed class AlertGridItem
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public AlertLevel Level { get; init; }
    public bool IsRead { get; init; }
    public bool IsResolved { get; init; }
    public DateTime CreatedAt { get; init; }
    public string DedupKey { get; init; } = string.Empty;
}

internal static class AlertLevelFilterExtensions
{
    public static AlertLevel? ToAlertLevel(this string? value)
    {
        if(string.IsNullOrWhiteSpace(value)) return null;
        if(Enum.TryParse<AlertLevel>(value,true,out var parsed)) return parsed;

        return value switch
        {
            "Критични" => AlertLevel.Critical,
            "Предупредувања" => AlertLevel.Warning,
            "Информативни" => AlertLevel.Info,
            _ => null
        };
    }
}
