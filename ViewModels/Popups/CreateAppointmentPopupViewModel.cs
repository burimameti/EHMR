using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Popups;

public partial class CreateAppointmentPopupViewModel : ObservableObject
{
    private readonly IEncounterDetailService _encounterService;
    private readonly IAuthorizationService _authorizationService;
    private readonly Guid _patientId;
    private readonly Guid? _prefillDoctorId;
    public Action<object?>? RequestClose
    {
        get; set;
    }


    public CreateAppointmentPopupViewModel(
        IEncounterDetailService encounterService,
        IAuthorizationService authorizationService,
        Guid patientId,
        Guid? docId)
    {
        _encounterService=encounterService;
        _authorizationService=authorizationService;
        _patientId=patientId;
        _prefillDoctorId=docId;




        AppointmentDate=DateTime.Today;
        AppointmentTime=new TimeSpan(9, 0, 0);
    }


    [ObservableProperty]
    private ObservableCollection<Doctor> doctors = new();


    [ObservableProperty]
    private Doctor? selectedDoctor;


    [ObservableProperty]
    private DateTime appointmentDate;


    [ObservableProperty]
    private TimeSpan appointmentTime;


    [ObservableProperty]
    private string reasonForVisit = string.Empty;


    [ObservableProperty]
    private bool hasError;


    [ObservableProperty]
    private string errorMessage = string.Empty;


    [ObservableProperty]
    private bool isBusy;


    [RelayCommand]
    private async Task SaveAsync()
    {
        if(SelectedDoctor==null)
        {
            HasError=true;
            ErrorMessage="Изберете реуматолог";
            return;
        }

        if(!_authorizationService.CanPerform(Modules.Appointments, ModuleAction.Create) ||
           !_authorizationService.CanPerform(Modules.Appointments, ModuleAction.Schedule))
        {
            HasError=true;
            ErrorMessage="Немате овластување за креирање и закажување термин.";
            return;
        }

        var requestedStart=AppointmentDate.Date+AppointmentTime;
        if(requestedStart<=DateTime.Now)
        {
            HasError=true;
            ErrorMessage="Изберете датум и време во иднина.";
            return;
        }

        if(IsBusy)
            return;


        try
        {
            IsBusy=true;
            HasError=false;

            var appointment = new Appointment
            {
                Id=Guid.NewGuid(),
                PatientId=_patientId,
                DoctorId=SelectedDoctor.Id,
                ScheduledStart=requestedStart,
                ScheduledEnd=requestedStart.AddMinutes(30),
                ReasonForVisit=ReasonForVisit.Trim(),
                Status=AppointmentStatus.Scheduled
            };

            var availableStart=await _encounterService.GetNextAvailableSlot(
                SelectedDoctor.Id, requestedStart, 30, _patientId);
            if(availableStart!=requestedStart)
            {
                HasError=true;
                ErrorMessage=$"Избраниот термин не е слободен. Најблизок слободен термин е {availableStart:dd.MM.yyyy HH:mm}.";
                return;
            }

            var saved =
                await _encounterService.CreateAppointment(appointment);


            RequestClose?.Invoke(saved);
        }
        catch(Exception ex)
        {
            HasError=true;
            ErrorMessage=$"Грешка при зачувување: {ex.Message}";
        }
        finally
        {
            IsBusy=false;
        }
    }
    public async Task InitializeAsync()
    {
        var doctors = await _encounterService.GetDoctors();
        Doctors=new ObservableCollection<Doctor>(doctors);
        SelectedDoctor=Doctors.FirstOrDefault(d => d.Id==_prefillDoctorId);

        if(SelectedDoctor is not null)
        {
            var nextSlot=await _encounterService.GetNextAvailableSlot(
                SelectedDoctor.Id, DateTime.Now, 30, _patientId);
            AppointmentDate=nextSlot.Date;
            AppointmentTime=nextSlot.TimeOfDay;
        }
        else
        {
            var now=DateTime.Now;
            var nextSlot=new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            var minutesToNextHalfHour=30-(nextSlot.Minute%30);
            nextSlot=nextSlot.AddMinutes(minutesToNextHalfHour);
            AppointmentDate=nextSlot.Date;
            AppointmentTime=nextSlot.TimeOfDay;
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        RequestClose?.Invoke(null);
    }
}