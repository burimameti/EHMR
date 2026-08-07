using System;
using System.Collections.Generic;

namespace EHMR.Backups.Scheduler;

/// <summary>
/// Минимален вреднувач на стандарден cron израз со 5 полиња:
/// <c>минута час ден-во-месец месец ден-во-недела</c>.
///
/// Проектот нема библиотека за cron, а `Backup:ScheduleCron` досега воопшто
/// не се читаше. Ова покрива што реално се користи за распоред на копии:
/// точна вредност (<c>2</c>), сите (<c>*</c>), листа (<c>1,15</c>),
/// опсег (<c>1-5</c>) и чекор (<c>*/15</c>, <c>0-30/10</c>).
///
/// Ден-во-недела е 0–6 со недела на 0; 7 се прифаќа како недела.
/// </summary>
public sealed class CronSchedule
{
    private readonly HashSet<int> _minutes;
    private readonly HashSet<int> _hours;
    private readonly HashSet<int> _daysOfMonth;
    private readonly HashSet<int> _months;
    private readonly HashSet<int> _daysOfWeek;

    public string Expression
    {
        get;
    }

    private CronSchedule(
        string expression,
        HashSet<int> minutes,
        HashSet<int> hours,
        HashSet<int> daysOfMonth,
        HashSet<int> months,
        HashSet<int> daysOfWeek)
    {
        Expression=expression;
        _minutes=minutes;
        _hours=hours;
        _daysOfMonth=daysOfMonth;
        _months=months;
        _daysOfWeek=daysOfWeek;
    }

    /// <summary>Враќа false наместо да фрли — неисправен израз не смее да сруши подигање.</summary>
    public static bool TryParse(string? expression, out CronSchedule? schedule)
    {
        schedule=null;

        if(string.IsNullOrWhiteSpace(expression))
            return false;

        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if(parts.Length!=5)
            return false;

        if(!TryParseField(parts[0], 0, 59, out var minutes)) return false;
        if(!TryParseField(parts[1], 0, 23, out var hours)) return false;
        if(!TryParseField(parts[2], 1, 31, out var daysOfMonth)) return false;
        if(!TryParseField(parts[3], 1, 12, out var months)) return false;
        if(!TryParseField(parts[4], 0, 7, out var daysOfWeek)) return false;

        // 7 и 0 се иста работа — недела.
        if(daysOfWeek!.Contains(7))
        {
            daysOfWeek.Remove(7);
            daysOfWeek.Add(0);
        }

        schedule=new CronSchedule(
            expression.Trim(),
            minutes!,
            hours!,
            daysOfMonth!,
            months!,
            daysOfWeek);

        return true;
    }

    /// <summary>Дали изразот се совпаѓа со дадениот момент, во точност на минута.</summary>
    public bool Matches(DateTime moment)
        => _minutes.Contains(moment.Minute)
        && _hours.Contains(moment.Hour)
        && _daysOfMonth.Contains(moment.Day)
        && _months.Contains(moment.Month)
        && _daysOfWeek.Contains((int)moment.DayOfWeek);

    private static bool TryParseField(string field, int min, int max, out HashSet<int>? values)
    {
        values=new HashSet<int>();

        foreach(var part in field.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var step = 1;
            var body = part;

            var slash = part.IndexOf('/');
            if(slash>=0)
            {
                body=part[..slash];

                if(!int.TryParse(part[(slash+1)..], out step)||step<=0)
                {
                    values=null;
                    return false;
                }
            }

            int rangeStart;
            int rangeEnd;

            if(body=="*")
            {
                rangeStart=min;
                rangeEnd=max;
            }
            else
            {
                var dash = body.IndexOf('-');

                if(dash>=0)
                {
                    if(!int.TryParse(body[..dash], out rangeStart)||
                       !int.TryParse(body[(dash+1)..], out rangeEnd))
                    {
                        values=null;
                        return false;
                    }
                }
                else
                {
                    if(!int.TryParse(body, out rangeStart))
                    {
                        values=null;
                        return false;
                    }

                    rangeEnd=rangeStart;
                }
            }

            if(rangeStart<min||rangeEnd>max||rangeStart>rangeEnd)
            {
                values=null;
                return false;
            }

            for(var value = rangeStart; value<=rangeEnd; value+=step)
                values.Add(value);
        }

        return values.Count>0;
    }
}
