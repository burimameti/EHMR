using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

public partial class DashboardAppointmentItem : ObservableObject
{
    [ObservableProperty]
    private string patientName = string.Empty;

    [ObservableProperty]
    private DateTime scheduledStart;

    [ObservableProperty]
    private string time = string.Empty;

    [ObservableProperty]
    private string relativeDay
  = string.Empty;

    [ObservableProperty]
    private string statusColor
    = string.Empty;
}