using EHMR.Domain.Entities;

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