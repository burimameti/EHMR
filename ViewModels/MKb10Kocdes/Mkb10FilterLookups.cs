using EHMR.Domain.Entities;
using EHMR.ViewModels.Patients.Extensions;


namespace EHMR.ViewModels.Mkb10
{
   

    /// <summary>
    /// Static factory for MKB-10 filter lookups. Chapter is data-driven since
    /// chapters come from whatever distinct values exist in the Mkb10Codes table,
    /// not a fixed enum — mirrors PatientFilterLookups.BuildCityLookup().
    /// </summary>
    public static class Mkb10FilterLookups
    {
        public static FilterLookup Status
        {
            get;
        } = new(new[]
        {
        ("Сите", "All"),
        ("Активен", "Active"),
        ("Неактивен", "Inactive")
    });

        /// <summary>
        /// Built dynamically from the loaded Mkb10Code list (distinct Chapter values),
        /// since chapters aren't a fixed enum — call this AFTER AllItems is populated
        /// in LoadAsync, same timing as BuildCityLookup relative to patient load.
        /// </summary>
        public static FilterLookup BuildChapterLookup(IEnumerable<Mkb10Code> allCodes)
        {
            var pairs = new List<(string Display, string Internal)> { ("Сите", "All") };

            var distinctChapters = allCodes
                .Where(x => !string.IsNullOrWhiteSpace(x.Chapter))
                .Select(x => x.Chapter!.Trim())
                .Distinct()
                .OrderBy(x => x);

            foreach(var chapter in distinctChapters)
            {
                pairs.Add((chapter, chapter));
            }

            return new FilterLookup(pairs);
        }
    }
}