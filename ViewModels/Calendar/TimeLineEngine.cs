//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace EHMR.ViewModels.Calendar
//{
//    public class TimelineEngine : ITimelineEngine
//    {
//        public List<HourlyTimelineSlotDto> BuildDay(DateTime date, List<CalendarEventDto> events)
//        {
//            var slots = new List<HourlyTimelineSlotDto>();

//            var start = TimeSpan.FromHours(8);
//            var end = TimeSpan.FromHours(20);

//            while(start<end)
//            {
//                var next = start.Add(TimeSpan.FromMinutes(30));

//                var slotEvents = events
//                    .Where(e =>
//                        e.EventType==CalendarEventType.Appointment&&
//                        e.ScheduledTime.TimeOfDay>=start&&
//                        e.ScheduledTime.TimeOfDay<next)
//                    .ToList();

//                slots.Add(new HourlyTimelineSlotDto
//                {
//                    StartTime=start,
//                    EndTime=next,
//                    Events=slotEvents
//                });

//                start=next;
//            }

//            return slots;
//        }
//    }
//}