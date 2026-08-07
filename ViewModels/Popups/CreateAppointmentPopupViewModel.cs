using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Services;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Popups;

public partial class CreateAppointmentPopupViewModel : ObservableObject
{
    private readonly IEncounterDetailService _encounterService;
    private readonly Guid _patientId;
    private readonly Guid? _prefillDoctorId;
    public Action<object?>? RequestClose
    {
        get; set;
    }


    public CreateAppointmentPopupViewModel(
        IEncounterDetailService encounterService,
        Guid patientId,
        Guid? docId)
    {
        _encounterService=encounterService;
        _patientId=patientId;




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
            ErrorMessage="Изберете лекар";
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
                ScheduledStart=AppointmentDate.Date+AppointmentTime,
                ReasonForVisit=ReasonForVisit,
                Status=AppointmentStatus.Scheduled
            };


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
    }

    [RelayCommand]
    public void Cancel()
    {
        RequestClose?.Invoke(null);
    }
}