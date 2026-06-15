using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Constants;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

public partial class AppointmentDetailFormViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly INavigationService _navigationService;
    private readonly ISelectedItemService<Appointment> _selectedItemService;
    private readonly ISelectedItemService<Patient> _patientSelectionService; // Додаден сервис за комуникација

    private bool _isNewMode;

    [ObservableProperty] private Appointment _appointment = new();
    [ObservableProperty] private Patient _patient = new();
    [ObservableProperty] private string _pageTitle = string.Empty;
    [ObservableProperty] private bool _isReadOnly = true;
    [ObservableProperty] private bool _isEditMode;

    [ObservableProperty] private TimeSpan _selectedStartTime;
    [ObservableProperty] private TimeSpan _selectedEndTime;

    [ObservableProperty] private Patient? _selectedPatientForAppointment;
    [ObservableProperty] private Doctor? _selectedDoctorForAppointment;

    [ObservableProperty] private Color _inputBgColor = Color.FromArgb("#F8FAFC");
    [ObservableProperty] private Color _inputBorderColor = Color.FromArgb("#E2E8F0");

    public ObservableCollection<Patient> PatientsList { get; set; } = new();
    public ObservableCollection<Doctor> DoctorsList { get; set; } = new();
    public List<string> StatusOptions { get; set; } = Enum.GetNames(typeof(AppointmentStatus)).ToList();

    public bool CanCreateSchedule => !IsReadOnly&&!_isNewMode;

    public AppointmentDetailFormViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        ISelectedItemService<Appointment> selectedItemService,
        ISelectedItemService<Patient> patientSelectionService) // Ињектиран сервис
    {
        _dbFactory=dbFactory;
        _navigationService=navigationService;
        _selectedItemService=selectedItemService;
        _patientSelectionService=patientSelectionService;

        _=LoadInitialDataAsync();
    }

    public async Task LoadInitialDataAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var patients = await db.Patients.Where(p => !p.IsDeleted).AsNoTracking().ToListAsync();
            var doctors = await db.Doctors.AsNoTracking().ToListAsync();

            PatientsList.Clear();
            foreach(var p in patients) PatientsList.Add(p);

            DoctorsList.Clear();
            foreach(var d in doctors) DoctorsList.Add(d);

            InitializeForm();
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading lookups: {ex.Message}");
        }
    }

    /// <summary>
    /// Ова треба да го повикаш во Code-Behind на страницата во OnAppearing() методот
    /// за да фатиме кога корисникот се враќа од формата за нов пациент!
    /// </summary>
    public void CheckAndApplyReturnedPatient()
    {
        var returnedPatient = _patientSelectionService.SelectedItem;
        if(returnedPatient!=null&&returnedPatient.Id!=Guid.Empty&&returnedPatient.FirstName!="APPOINTMENT_CONTEXT")
        {
            // Провери дали веќе го има во локалната листа (за секој случај)
            var existing = PatientsList.FirstOrDefault(p => p.Id==returnedPatient.Id);
            if(existing==null)
            {
                PatientsList.Add(returnedPatient);
                SelectedPatientForAppointment=returnedPatient;
            }
            else
            {
                SelectedPatientForAppointment=existing;
            }

            // Го чистиме за да не се ресетира селекцијата при следно отворање
            _patientSelectionService.SelectedItem=null;
        }
    }

    private void InitializeForm()
    {
        var sharedAppointment = _selectedItemService.SelectedItem;

        if(sharedAppointment==null)
        {
            Appointment=new Appointment
            {
                ScheduledStart=DateTime.Today.AddHours(9),
                ScheduledEnd=DateTime.Today.AddHours(9).AddMinutes(30),
                Status=AppointmentStatus.Scheduled
            };
            _isNewMode=true;
            IsReadOnly=false;
            IsEditMode=true;
            PageTitle="➕ Нов Термин";
        }
        else if(sharedAppointment.Id==Guid.Empty)
        {
            Appointment=sharedAppointment;
            if(Appointment.ScheduledEnd<=Appointment.ScheduledStart)
            {
                Appointment.ScheduledEnd=Appointment.ScheduledStart.AddMinutes(30);
            }
            _isNewMode=true;
            IsReadOnly=false;
            IsEditMode=true;
            PageTitle="➕ Нов Термин од Календар";
        }
        else
        {
            Appointment=sharedAppointment;
            _isNewMode=false;
            IsReadOnly=true;
            IsEditMode=false;
            PageTitle="Преглед на Термин";
        }

        SelectedStartTime=Appointment.ScheduledStart.TimeOfDay;
        SelectedEndTime=Appointment.ScheduledEnd.TimeOfDay;

        SelectedPatientForAppointment=PatientsList.FirstOrDefault(p => p.Id==Appointment.PatientId);
        SelectedDoctorForAppointment=DoctorsList.FirstOrDefault(d => d.Id==Appointment.DoctorId);

        UpdateInputStyle();
    }

    [RelayCommand]
    private async Task NavigateToAddNewPatientAsync()
    {
        // Поставуваме маркер објект за PatientForm да знае дека треба да нè врати тука со зачуваната вредност
        _patientSelectionService.SelectedItem=new Patient
        {
            Id=Guid.Empty,
            FirstName="APPOINTMENT_CONTEXT"
        };

        // Навигација кон формата за пациенти
        await _navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

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

    [RelayCommand]
    private async Task SaveAsync()
    {
        if(SelectedPatientForAppointment==null||SelectedDoctorForAppointment==null)
        {
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            Appointment.ScheduledStart=Appointment.ScheduledStart.Date+SelectedStartTime;
            Appointment.ScheduledEnd=Appointment.ScheduledEnd.Date+SelectedEndTime;
            Appointment.PatientId=SelectedPatientForAppointment.Id;
            Appointment.DoctorId=SelectedDoctorForAppointment.Id;

            if(_isNewMode)
            {
                Appointment.Id=Guid.NewGuid();
                await db.Appointments.AddAsync(Appointment);
            }
            else
            {
                db.Appointments.Update(Appointment);
            }

            await db.SaveChangesAsync();
            _selectedItemService.SelectedItem=null;
            await _navigationService.GoToAsync("..");
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving appointment: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task GenerateNextTherapyCycleAndAppointmentAsync()
    {
        if(Appointment==null||Appointment.PatientId==Guid.Empty) return;
        if(Patient==null||Patient.Id==Guid.Empty) return;
        await using var db = await _dbFactory.CreateDbContextAsync();

        var activeSchedule = await db.TherapySchedules
            .FirstOrDefaultAsync(s => s.TreatmentPlan.PatientId==Appointment.PatientId&&s.IsActive);

        if(activeSchedule==null)
        {
            await App.Current.MainPage.DisplayAlert("Предупредување", "Не е пронајден активен терапевтски распоред за овој пациент.", "Во ред");
            return;
        }

        DateTime idealStartDate = DateTime.Today.AddDays(activeSchedule.FrequencyInDays);
        DateTime finalScheduledStart = await FindFirstAvailableSlotAsync(db, Appointment.DoctorId, idealStartDate);
        DateTime finalScheduledEnd = finalScheduledStart.AddMinutes(30);

        var lastCycle = await db.TherapyCycles
            .Where(c => c.TherapyScheduleId==activeSchedule.Id)
            .OrderByDescending(c => c.CycleNumber)
            .FirstOrDefaultAsync();

        int nextCycleNumber = (lastCycle?.CycleNumber??0)+1;

        var newCycle = new TherapyCycle
        {
            Id=Guid.NewGuid(),
            TherapyScheduleId=activeSchedule.Id,
            CycleNumber=nextCycleNumber,
            PlannedStartDate=finalScheduledStart.Date,
            PlannedEndDate=finalScheduledStart.Date.AddDays(activeSchedule.FrequencyInDays-1),
            Status=TherapyStatus.Planned
        };
        db.TherapyCycles.Add(newCycle);

        var futureAppointment = new Appointment
        {
            Id=Guid.NewGuid(),
            PatientId=Appointment.PatientId,
            DoctorId=Appointment.DoctorId,
            ScheduledStart=finalScheduledStart,
            ScheduledEnd=finalScheduledEnd,
            ReasonForVisit=$"Автоматски алоциран термин за Циклус #{nextCycleNumber}",
            TherapyCycleId=newCycle.Id,
            Status=AppointmentStatus.Scheduled
        };
        db.Appointments.Add(futureAppointment);

        await db.SaveChangesAsync();
        await App.Current.MainPage.DisplayAlert("Успешно", "Креиран е нов паметен термин во календарот.", "Одлично");
    }

    private async Task<DateTime> FindFirstAvailableSlotAsync(DesktopTherapyDbContext db, Guid doctorId, DateTime startingDate)
    {
        DateTime candidateDate = startingDate;
        int workStartHour = 8;
        int workEndHour = 16;
        int slotDurationMinutes = 30;

        for(int dayOffset = 0; dayOffset<90; dayOffset++)
        {
            if(candidateDate.DayOfWeek==DayOfWeek.Saturday||candidateDate.DayOfWeek==DayOfWeek.Sunday)
            {
                candidateDate=candidateDate.AddDays(1);
                continue;
            }

            var bookedAppointments = await db.Appointments
                .Where(a => a.DoctorId==doctorId&&a.ScheduledStart.Date==candidateDate.Date&&a.Status!=AppointmentStatus.Cancelled)
                .Select(a => new { a.ScheduledStart, a.ScheduledEnd })
                .ToListAsync();

            for(int hour = workStartHour; hour<workEndHour; hour++)
            {
                for(int minute = 0; minute<60; minute+=slotDurationMinutes)
                {
                    DateTime possibleStart = new DateTime(candidateDate.Year, candidateDate.Month, candidateDate.Day, hour, minute, 0);
                    DateTime possibleEnd = possibleStart.AddMinutes(slotDurationMinutes);

                    bool isOverlapping = bookedAppointments.Any(booked =>
                        (possibleStart>=booked.ScheduledStart&&possibleStart<booked.ScheduledEnd)||
                        (possibleEnd>booked.ScheduledStart&&possibleEnd<=booked.ScheduledEnd)
                    );

                    if(!isOverlapping) return possibleStart;
                }
            }
            candidateDate=candidateDate.AddDays(1);
        }
        return startingDate.Date.AddHours(8);
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        _selectedItemService.SelectedItem=null;
        await _navigationService.GoToAsync("..");
    }

    [RelayCommand]
    private async Task NavigateToCreateScheduleAsync()
    {
        await _navigationService.GoToAsync(AppRoutes.Cycle.Details);
    }

    private void UpdateInputStyle()
    {
        InputBgColor=IsReadOnly ? Color.FromArgb("#F1F5F9") : Color.FromArgb("#FFFFFF");
        InputBorderColor=IsReadOnly ? Color.FromArgb("#CBD5E1") : Color.FromArgb("#2563EB");
    }
}