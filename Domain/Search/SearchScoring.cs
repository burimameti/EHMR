using EHMR.Services;

namespace EHMR.Domain.Search
{
    internal static class SearchScoring
    {
        public static double Score(string text, string query)
        {
            if(string.IsNullOrWhiteSpace(text)||string.IsNullOrWhiteSpace(query))
                return 0;

            text=text.ToLowerInvariant();
            query=query.ToLowerInvariant();

            if(text==query) return 100;
            if(text.StartsWith(query)) return 85;
            if(text.Contains(query)) return 60;

            return 0;
        }

        public static int TypeBoost(SearchEntityType type) => type switch
        {
            SearchEntityType.Patient => 30,
            SearchEntityType.Doctor => 20,
            SearchEntityType.Appointment => 10,
            _ => 0
        };
    }
}