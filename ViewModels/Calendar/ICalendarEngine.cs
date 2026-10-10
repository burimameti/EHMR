using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Calendar
{
    public interface ICalendarEngine { }

    public interface ITimelineEngine
    {
        List<HourlyTimelineSlotDto> BuildDay(DateTime date, List<CalendarEventDto> events);
    }
}