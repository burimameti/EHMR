using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Calendar
{
    public interface ICalendarEngine
    {
        List<CalendarDayDto> BuildMonth(
            DateTime month,
            List<TherapyCycle> cycles,
            List<Appointment> appointments);

        List<CalendarEventDto> BuildEvents(
           List<TherapyCycle> cycles,
           List<Appointment> appointments);
    }

    public interface ITimelineEngine
    {
        List<HourlyTimelineSlotDto> BuildDay(DateTime date, List<CalendarEventDto> events);
    }
}