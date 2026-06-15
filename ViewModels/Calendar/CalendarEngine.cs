//using EHMR.Domain.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace EHMR.ViewModels.Calendar
//{
//    public class CalendarEngine : ICalendarEngine
//    {
//        public List<CalendarDayDto> BuildMonth(
//            DateTime month,
//            List<TherapyCycle> cycles,
//            List<Appointment> appointments)
//        {
//            var startOfMonth = new DateTime(month.Year, month.Month, 1);
//            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

//            var days = new List<CalendarDayDto>();

//            int offset = ((int)startOfMonth.DayOfWeek+6)%7;

//            for(int i = 0; i<offset; i++)
//            {
//                days.Add(new CalendarDayDto());
//            }

//            for(int d = 1; d<=endOfMonth.Day; d++)
//            {
//                var date = new DateTime(month.Year, month.Month, d);

//                var dayCycles = cycles
//                    .Where(c => date>=c.PlannedStartDate&&date<=c.PlannedEndDate)
//                    .ToList();

//                var dayAppointments = appointments
//                    .Where(a => a.ScheduledStart.Date==date.Date)
//                    .ToList();

//                days.Add(new CalendarDayDto
//                {
//                    DayNumber=d,
//                    IsToday=date.Date==DateTime.Today,
//                    Events=BuildEvents(dayCycles, dayAppointments),
//                    AppointmentCount=dayAppointments.Count,
//                    CompletedCount=dayAppointments.Count(x => x.Status==AppointmentStatus.Completed),
//                    MissedCount=dayAppointments.Count(x => x.Status==AppointmentStatus.Missed)
//                });
//            }

//            return days;
//        }

//        public List<CalendarEventDto> BuildEvents(
//            List<TherapyCycle> cycles,
//            List<Appointment> appointments)
//        {
//            var events = new List<CalendarEventDto>();

//            events.AddRange(cycles.Select(c => new CalendarEventDto
//            {
//                Id=c.Id,
//                EventType=CalendarEventType.TherapyCycle,
//                Title=$"{c.TherapySchedule.Name} C#{c.CycleNumber}",
//                ScheduledTime=c.PlannedStartDate,
//                Status=c.Status.ToString()
//            }));

//            events.AddRange(appointments.Select(a => new CalendarEventDto
//            {
//                Id=a.Id,
//                EventType=CalendarEventType.TherapyCycle,
//                Title=$"{a.Patient.LastName} {a.ScheduledStart:HH:mm}",
//                ScheduledTime=a.ScheduledStart,
//                Status=a.Status.ToString()
//            }));

//            return events
//                .OrderBy(e => e.ScheduledTime)
//                .ToList();
//        }
//    }
//}