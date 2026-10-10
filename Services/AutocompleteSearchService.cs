using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EHMR.Services;

public enum SearchEntityType
{
    Patient,
    Doctor,
    Appointment,
    PatientMkb10Assignment,
    Medication
}

public sealed class SearchSuggestion
{
    public string Id { get; set; } = string.Empty;

    public string DisplayText { get; set; } = string.Empty;

    public SearchEntityType Type
    {
        get; set;
    }

    public double Score
    {
        get; set;
    }

    public string Group => Type switch
    {
        SearchEntityType.Patient => "Patients",
        SearchEntityType.Doctor => "Doctors",
        SearchEntityType.Appointment => "Appointments",
        SearchEntityType.PatientMkb10Assignment => "Diagnoses",
        SearchEntityType.Medication => "Medications",
        _ => "Other"
    };
}

public interface IAutocompleteSearchService
{
    Task<IReadOnlyList<SearchSuggestion>> SearchAsync(
        string query,
        SearchEntityType[] types,
        int take = 10,
        CancellationToken ct = default);
}

public class AutocompleteSearchService : IAutocompleteSearchService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

    public AutocompleteSearchService(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
    {
        _dbFactory=dbFactory;
    }

    // =========================
    // THREAD-SAFE CACHE (TTL)
    // =========================
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private sealed class CacheEntry
    {
        public IReadOnlyList<SearchSuggestion> Results { get; init; } = [];

        public DateTime CreatedAt
        {
            get; init;
        }
    }

    // =========================
    // NORMALIZATION
    // =========================
    private static string Normalize(string input)
        => input.Trim().ToLowerInvariant();

    // =========================
    // SCORING ENGINE (GOOGLE STYLE)
    // =========================
    private static double ScoreMatch(string text, string query)
    {
        if(string.IsNullOrWhiteSpace(text)||
            string.IsNullOrWhiteSpace(query))
            return 0;

        text=text.ToLowerInvariant();
        query=query.ToLowerInvariant();

        if(text==query)
            return 100;

        if(text.StartsWith(query))
            return 90;

        if(text.Split(' ')
                .Any(x => x.StartsWith(query)))
            return 80;

        if(text.Contains(query))
            return 60;

        return 0;
    }

    private static int TypeBoost(SearchEntityType type) => type switch
    {
        SearchEntityType.Patient => 40,
        SearchEntityType.Doctor => 30,
        SearchEntityType.Appointment => 20,
        SearchEntityType.PatientMkb10Assignment => 10,
        SearchEntityType.Medication => 5,
        _ => 0
    };

    // =========================
    // MAIN SEARCH
    // =========================
    public async Task<IReadOnlyList<SearchSuggestion>> SearchAsync(
        string query,
        SearchEntityType[] types,
        int take = 10,
        CancellationToken ct = default)
    {
        if(string.IsNullOrWhiteSpace(query))
            return [];

        query=query.Trim();
        var key =
     $"{Normalize(query)}_{string.Join(",", types.OrderBy(x => x))}";

        // =========================
        // CACHE HIT
        // =========================
        if(_cache.TryGetValue(key, out var cached))
        {
            if(DateTime.UtcNow-cached.CreatedAt<CacheTtl)
                return cached.Results;

            _cache.TryRemove(key, out _);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var results = new List<SearchSuggestion>();

        // =========================
        // PATIENTS
        // =========================
        if(types.Contains(SearchEntityType.Patient))
        {
            var patients = await db.Patients
                .AsNoTracking()
                .Where(p =>
       p.FirstName.Contains(query)
    ||p.LastName.Contains(query)
    ||(p.FirstName+" "+p.LastName).Contains(query))
                .Take(8)
                .ToListAsync(ct);

            results.AddRange(patients.Select(p => new SearchSuggestion
            {
                Id=p.Id.ToString(),
                Type=SearchEntityType.Patient,
                DisplayText=$"{p.FirstName} {p.LastName}",
                Score=
                    ScoreMatch(p.FirstName, query)+
                    ScoreMatch(p.LastName, query)+
                    TypeBoost(SearchEntityType.Patient)
            }));
        }

        // =========================
        // DOCTORS
        // =========================
        if(types.Contains(SearchEntityType.Doctor))
        {
            var doctors = await db.Doctors
                .AsNoTracking()
                .Where(d =>
                    d.User.FirstName.StartsWith(query)||
                    d.User.LastName.StartsWith(query))
                .Take(6)
                .ToListAsync(ct);

            results.AddRange(doctors.Select(d => new SearchSuggestion
            {
                Id=d.Id.ToString(),
                Type=SearchEntityType.Doctor,
                DisplayText=$"Dr. {d.User.FirstName} {d.User.LastName}",
                Score=
                    ScoreMatch(d.User.FirstName, query)+
                    ScoreMatch(d.User.LastName, query)+
                    TypeBoost(SearchEntityType.Doctor)
            }));
        }

        // =========================
        // APPOINTMENTS
        // =========================
        if(types.Contains(SearchEntityType.Appointment))
        {
            var appts = await db.Appointments
                .Include(a => a.Patient)
                .AsNoTracking()
                .Where(a =>
                    a.Patient.FirstName.StartsWith(query)||
                    a.Patient.LastName.StartsWith(query))
                .Take(5)
                .ToListAsync(ct);

            results.AddRange(appts.Select(a => new SearchSuggestion
            {
                Id=a.Id.ToString(),
                Type=SearchEntityType.Appointment,
                DisplayText=$"{a.Patient.FirstName} {a.Patient.LastName} • {a.ScheduledStart:dd.MM}",
                Score=40+TypeBoost(SearchEntityType.Appointment)
            }));
        }
        if(types.Contains(SearchEntityType.PatientMkb10Assignment))
        {
            var diagnoses = await db.Mkb10Codes
                .AsNoTracking()
                .Where(x =>
                    x.Code.Contains(query)||
                    x.Description.Contains(query))
                .Take(10)
                .ToListAsync(ct);

            results.AddRange(
                diagnoses.Select(x => new SearchSuggestion
                {
                    Id=x.Id.ToString(),
                    Type=SearchEntityType.PatientMkb10Assignment,
                    DisplayText=
                        $"{x.Code} • {x.Description}",
                    Score=
                        ScoreMatch(x.Code, query)
                        +ScoreMatch(x.Description, query)
                        +TypeBoost(SearchEntityType.PatientMkb10Assignment)
                }));
        }
        // =========================
        // FINAL RANKING (GOOGLE STYLE)
        // =========================
        var ordered = results
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.DisplayText)
            .Take(take)
            .ToList();

        // =========================
        // CACHE STORE
        // =========================
        _cache[key]=new CacheEntry
        {
            Results=ordered,
            CreatedAt=DateTime.UtcNow
        };

        // Optional cleanup (lightweight)
        if(_cache.Count>500)
        {
            foreach(var item in _cache
                .Where(x => DateTime.UtcNow-x.Value.CreatedAt>CacheTtl)
                .ToList())
            {
                _cache.TryRemove(item.Key, out _);
            }
        }

        return ordered;
    }
}