using EHMR.ViewModels.Patients.Extensions;

namespace EHMR.ViewModels.Encounters;

public static class EncounterFormLookups
{
    public static FilterLookup EncounterType
    {
        get;
    } = new(new[]
    {
        ("Амбулантски", "Outpatient"),
        ("Болнички", "Inpatient"),
        ("Итен случај", "Emergency"),
        ("Телемедицина", "Telehealth"),
        ("Контролен преглед", "FollowUp"),
    });

    public static FilterLookup Priority
    {
        get;
    } = new(new[]
    {
        ("Рутински", "Routine"),
        ("Итно", "Urgent"),
        ("Хитен случај", "Emergency"),
        ("STAT (Веднаш)", "STAT"),
    });
}