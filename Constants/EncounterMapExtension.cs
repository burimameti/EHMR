using System.Collections.Generic;
using System.Linq;

namespace EHMR.ViewModels.Constants;

public static class EncounterStatusSchema
{
    public static readonly Dictionary<string, string> Display = new()
    {
        ["Scheduled"]="Закажан",
        ["CheckedIn"]="Пријавен",
        ["Waiting"]="Чека",
        ["InProgress"]="Во тек",
        ["Completed"]="Завршен",
        ["Cancelled"]="Откажан",
        ["NoShow"]="Не дојде"
    };

    public static readonly Dictionary<string, string> Color = new()
    {
        ["Scheduled"]="#4F46E5",
        ["CheckedIn"]="#0EA5E9",
        ["Waiting"]="#F59E0B",
        ["InProgress"]="#22C55E",
        ["Completed"]="#16A34A",
        ["Cancelled"]="#EF4444",
        ["NoShow"]="#64748B"
    };

    public static readonly Dictionary<string, string> Background = new()
    {
        ["Scheduled"]="#EEF2FF",
        ["CheckedIn"]="#E0F2FE",
        ["Waiting"]="#FFFBEB",
        ["InProgress"]="#DCFCE7",
        ["Completed"]="#F0FDF4",
        ["Cancelled"]="#FEF2F2",
        ["NoShow"]="#F1F5F9"
    };

    public static readonly List<string> Keys = Display.Keys.ToList();
    public static readonly List<string> Values = Display.Values.ToList();

    public static string ToDisplay(string key)
    {
        if(key=="All") return "Сите";
        return Display.TryGetValue(key, out var v) ? v : key;
    }

    public static string ToKeyFromDisplay(string display)
    {
        if(string.IsNullOrWhiteSpace(display)||display=="Сите"||display=="All") return "All";
        return Display.FirstOrDefault(x => x.Value==display).Key??"All";
    }
}

public static class EncounterPrioritySchema
{
    public static readonly Dictionary<string, string> Display = new()
    {
        ["Routine"]="Рутински",
        ["Urgent"]="Итно",
        ["Emergency"]="Екстремно",
        ["STAT"]="СТАТ"
    };

    public static readonly Dictionary<string, string> Color = new()
    {
        ["Routine"]="#64748B",
        ["Urgent"]="#F59E0B",
        ["Emergency"]="#EF4444",
        ["STAT"]="#DC2626"
    };

    public static readonly Dictionary<string, string> Background = new()
    {
        ["Routine"]="#F1F5F9",
        ["Urgent"]="#FFFBEB",
        ["Emergency"]="#FEF2F2",
        ["STAT"]="#FEE2E2"
    };

    public static readonly List<string> Keys = Display.Keys.ToList();
    public static readonly List<string> Values = Display.Values.ToList();

    public static string ToDisplay(string key)
    {
        if(key=="All") return "Сите";
        return Display.TryGetValue(key, out var v) ? v : key;
    }

    public static string ToKeyFromDisplay(string display)
    {
        if(string.IsNullOrWhiteSpace(display)||display=="Сите"||display=="All") return "All";
        return Display.FirstOrDefault(x => x.Value==display).Key??"All";
    }
}

public static class EncounterTypeSchema
{
    public static readonly Dictionary<string, string> Display = new()
    {
        ["Outpatient"]="Амбулантски",
        ["Inpatient"]="Хоспитализиран",
        ["Emergency"]="Итна состојба",
        ["Telehealth"]="Телемедицина",
        ["FollowUp"]="Контрола"
    };

    public static readonly List<string> Keys = Display.Keys.ToList();
    public static readonly List<string> Values = Display.Values.ToList();

    public static string ToDisplay(string key)
    {
        if(key=="All") return "Сите";
        return Display.TryGetValue(key, out var v) ? v : key;
    }

    public static string ToKeyFromDisplay(string display)
    {
        if(string.IsNullOrWhiteSpace(display)||display=="Сите"||display=="All") return "All";
        return Display.FirstOrDefault(x => x.Value==display).Key??"All";
    }
}