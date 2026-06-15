using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Constants;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Prescriptions;

public partial class PrescriptionDetailFormViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly INavigationService _navigationService;
    private readonly ISelectedItemService<Prescription> _selectedItemService;

    private bool _isNewMode;

    [ObservableProperty] private Prescription _prescription = new();
    [ObservableProperty] private string _pageTitle = string.Empty;
    [ObservableProperty] private bool _isReadOnly = true;
    [ObservableProperty] private bool _isEditMode;

    [ObservableProperty] private Patient? _selectedPatient;
    [ObservableProperty] private Color _inputBgColor = Color.FromArgb("#F8FAFC");
    [ObservableProperty] private Color _inputBorderColor = Color.FromArgb("#E2E8F0");

    public ObservableCollection<Patient> PatientsList { get; set; } = new();

    public bool CanAddMedicine => !IsReadOnly;

    public PrescriptionDetailFormViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        ISelectedItemService<Prescription> selectedItemService)
    {
        _dbFactory=dbFactory;
        _navigationService=navigationService;
        _selectedItemService=selectedItemService;

        _=LoadInitialDataAsync();
    }

    // =========================================================
    // INIT
    // =========================================================

    private async Task LoadInitialDataAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var patients = await db.Patients
                .AsNoTracking()
                .ToListAsync();

            PatientsList.Clear();
            foreach(var p in patients)
                PatientsList.Add(p);

            InitializeForm();
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Prescription init error: {ex.Message}");
        }
    }

    private void InitializeForm()
    {
        var shared = _selectedItemService.SelectedItem;

        if(shared==null)
        {
            Prescription=new Prescription
            {
                Medicines=new List<PrescriptionMedicine>()
            };

            _isNewMode=true;
            IsReadOnly=false;
            IsEditMode=true;
            PageTitle="➕ Нова Прескрипција";
        }
        else if(shared.Id==Guid.Empty)
        {
            Prescription=shared;

            _isNewMode=true;
            IsReadOnly=false;
            IsEditMode=true;
            PageTitle="➕ Нова Прескрипција од Пациент";
        }
        else
        {
            Prescription=shared;

            _isNewMode=false;
            IsReadOnly=true;
            IsEditMode=false;
            PageTitle="Преглед на Прескрипција";
        }

        SelectedPatient=PatientsList.FirstOrDefault(p => p.Id==Prescription.PatientId);

        UpdateInputStyle();
    }

    // =========================================================
    // MODE
    // =========================================================

    [RelayCommand]
    private void ToggleEditMode()
    {
        IsReadOnly=false;
        IsEditMode=true;
        UpdateInputStyle();
    }

    [RelayCommand]
    private void Cancel()
    {
        if(_isNewMode)
        {
            _navigationService.GoToAsync("..");
        }
        else
        {
            InitializeForm();
        }
    }

    // =========================================================
    // MEDICINES
    // =========================================================

    [RelayCommand]
    private void AddMedicine()
    {
        Prescription.Medicines.Add(new PrescriptionMedicine
        {
            PrescriptionId=Prescription.Id,
            Dosage="1-0-1",
            Frequency="Daily",
            DurationDays=5
        });
    }

    [RelayCommand]
    private void RemoveMedicine(PrescriptionMedicine item)
    {
        Prescription.Medicines.Remove(item);
    }

    // =========================================================
    // SAVE
    // =========================================================

    [RelayCommand]
    private async Task SaveAsync()
    {
        if(SelectedPatient==null)
            return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            Prescription.PatientId=SelectedPatient.Id;

            if(_isNewMode)
            {
                Prescription.Id=Guid.NewGuid();
                db.Prescriptions.Add(Prescription);
            }
            else
            {
                db.Prescriptions.Update(Prescription);
            }

            await db.SaveChangesAsync();

            _selectedItemService.SelectedItem=null;
            await _navigationService.GoToAsync("..");
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save prescription error: {ex.Message}");
        }
    }

    // =========================================================
    // NAVIGATION
    // =========================================================

    [RelayCommand]
    private async Task BackAsync()
    {
        _selectedItemService.SelectedItem=null;
        await _navigationService.GoToAsync("..");
    }

    // =========================================================
    // UI STYLE
    // =========================================================

    private void UpdateInputStyle()
    {
        InputBgColor=IsReadOnly ? Color.FromArgb("#F1F5F9") : Color.FromArgb("#FFFFFF");
        InputBorderColor=IsReadOnly ? Color.FromArgb("#CBD5E1") : Color.FromArgb("#2563EB");
    }
}