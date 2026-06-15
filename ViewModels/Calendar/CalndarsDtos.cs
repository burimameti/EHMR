//using CommunityToolkit.Mvvm.ComponentModel;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace EHMR.ViewModels.Calendar
//{
//    public class QuickAppointmentRequestDto
//    {
//        public Guid PatientId
//        {
//            get; set;
//        }

//        public DateTimeOffset Start
//        {
//            get; set;
//        }

//        public DateTimeOffset End
//        {
//            get; set;
//        }

//        public Guid? DoctorId
//        {
//            get; set;
//        }

//        public string? Reason
//        {
//            get; set;
//        }
//    }

//    public class CalendarAlertDto
//    {
//        public Guid Id
//        {
//            get; set;
//        }

//        public string PatientName { get; set; } = string.Empty;

//        public string Context { get; set; } = string.Empty; // Cycle / Appointment / etc

//        public string Message { get; set; } = string.Empty;

//        public CalendarEventStatus Severity
//        {
//            get; set;
//        }

//        public DateTimeOffset DueDate
//        {
//            get; set;
//        }
//    }

//    public class CalendarDayDto
//    {
//        public int DayNumber
//        {
//            get; set;
//        }

//        public bool IsToday
//        {
//            get; set;
//        }

//        public bool IsIsEmptySlot
//        {
//            get; set;
//        }

//        public List<CalendarEventDto> Events { get; set; } = new();

//        // computed (no logic duplication in VM)
//        public int AppointmentCount
//        {
//            get; set;
//        }

//        public int CompletedCount
//        {
//            get; set;
//        }

//        public int MissedCount
//        {
//            get; set;
//        }

//        public bool HasAppointments => AppointmentCount>0;
//    }

//    public partial class HourlyTimelineSlotDto : ObservableObject
//    {
//        public TimeSpan StartTime
//        {
//            get; set;
//        }

//        public TimeSpan EndTime
//        {
//            get; set;
//        }

//        public bool IsProtocolRow
//        {
//            get; set;
//        }

//        public List<CalendarEventDto> Events { get; set; } = new();

//        [ObservableProperty]
//        private bool isSelected;

//        public string Display =>
//            IsProtocolRow
//                ? "PROTOCOLS"
//                : $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

//        public bool HasEvents => Events.Count>0;
//    }
//    public class PatientLookupDto
//    {
//        public Guid Id
//        {
//            get; set;
//        }

//        public string FirstName { get; set; } = string.Empty;
//        public string LastName { get; set; } = string.Empty;

//        public string FullName => $"{LastName} {FirstName}";
//    }
//    public class OverdueCycleDto
//    {
//        public string PatientName { get; set; } = string.Empty;

//        public string ScheduleName { get; set; } = string.Empty;

//        public string CycleInfo { get; set; } = string.Empty;

//        public string Status { get; set; } = string.Empty;
//    }

//    public partial class TimelineSlotDto : ObservableObject
//    {
//        public TimeRangeDto Range { get; set; } = new();

//        public List<CalendarEventDto> Events { get; set; } = new();

//        public CalendarEventType? PrimaryType =>
//            Events.Count==0 ? null :
//            Events.GroupBy(e => e.EventType)
//                   .OrderByDescending(g => g.Count())
//                   .First().Key;

//        public int LoadFactor => Events.Count;

//        public bool IsOverloaded => Events.Count>=3;

//        public bool IsProtocolRow
//        {
//            get; set;
//        }

//        [ObservableProperty]
//        private bool isSelected;
//    }

//    public class CycleOverdueAlertDto : CalendarAlertDto
//    {
//        public Guid TherapyCycleId
//        {
//            get; set;
//        }

//        public int CycleNumber
//        {
//            get; set;
//        }

//        public string ScheduleName { get; set; } = string.Empty;

//        public DateOnly PlannedEndDate
//        {
//            get; set;
//        }

//        public int DaysOverdue
//        {
//            get; set;
//        }

//        public bool IsCritical =>
//            DaysOverdue>7;

//        public string CycleInfo
//        {
//            get;
//            internal set;
//        }

//        public string Status
//        {
//            get;
//            internal set;
//        }
//    }

//    public class CalendarMonthDto
//    {
//        public DateOnly Month
//        {
//            get; set;
//        }

//        public List<CalendarDayDto> Days { get; set; } = new();

//        public string Title =>
//            Month.ToString("MMMM yyyy").ToUpperInvariant();
//    }

//    public partial class CalendarEventDto
//    {
//        public Guid Id
//        {
//            get; set;
//        }

//        // IMPORTANT: keep string (engine compatibility, avoids breaking EF mapping layers)
//        public CalendarEventType EventType { get; set; } = CalendarEventType.Appointment;

//        // "Appointment" | "Cycle"

//        public string Title { get; set; } = string.Empty;

//        public DateTime ScheduledTime
//        {
//            get; set;
//        }

//        public string Status { get; set; } = string.Empty;

//        public Guid? PatientId
//        {
//            get; set;
//        }
//    }

//    public class TimeRangeDto
//    {
//        public DateTimeOffset Start
//        {
//            get; set;
//        }

//        public DateTimeOffset End
//        {
//            get; set;
//        }

//        public TimeSpan Duration => End-Start;

//        public bool Overlaps(TimeRangeDto other)
//            => Start<other.End&&End>other.Start;
//    }

//    public enum CalendarEventType
//    {
//        Appointment,
//        TherapyCycle,
//        Protocol,
//        Task,
//        BlockedTime,
//        System
//    }

//    public enum CalendarEventStatus
//    {
//        Planned,
//        Confirmed,
//        Completed,
//        Cancelled,
//        Missed,
//        Overdue,
//        InProgress
//    }
//}