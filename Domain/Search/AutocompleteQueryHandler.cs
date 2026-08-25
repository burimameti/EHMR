using EHMR.Infrastructure.Persistence;
using EHMR.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace EHMR.Domain.Search
{
    public sealed class AppointmentSearchQueryHandler : IAppointmentSearchQueryHandler
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

        public AppointmentSearchQueryHandler(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
        {
            _dbFactory=dbFactory;
        }

        public async Task<IReadOnlyList<SearchSuggestionDto>> Handle(
            AppointmentSearchQuery request,
            CancellationToken ct)
        {
            if(string.IsNullOrWhiteSpace(request.Query))
                return [];

            var q = request.Query.Trim();

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var results = new List<SearchSuggestionDto>();

            // ================= PATIENT =================
            if(request.Types.Contains(SearchEntityType.Patient))
            {
                var patients = await db.Patients
                    .AsNoTracking()
                    .Where(p =>
                        p.FirstName.Contains(q)||
                        p.LastName.Contains(q))
                    .Select(p => new SearchSuggestionDto
                    {
                        Id=p.Id.ToString(),
                        Type=SearchEntityType.Patient,
                        DisplayText=$"{p.FirstName} {p.LastName} | Град: {p.City} | Адреса: {p.Address}",
                        Score=100
                    })
                    .ToListAsync(ct);

                results.AddRange(patients);
            }
            return results
              .OrderByDescending(x => x.Score)
              .Take(request.Take)
              .ToList();
            // ================= DOCTOR =================
            //if(request.Types.Contains(SearchEntityType.Doctor))
            //{
            //    var doctors = await db.Doctors
            //        .AsNoTracking()
            //        .Where(d =>
            //            d.User.FirstName.Contains(q)||
            //            d.User.LastName.Contains(q))
            //        .Select(d => new SearchSuggestionDto
            //        {
            //            Id=d.Id.ToString(),
            //            Type=SearchEntityType.Doctor,
            //            DisplayText="Dr. "+d.User.FirstName+" "+d.User.LastName,
            //            Score=80
            //        })
            //        .ToListAsync(ct);

            //    results.AddRange(doctors);
            //}

            // ================= APPOINTMENTS =================
            //if(request.Types.Contains(SearchEntityType.Appointment))
            //{
            //    var appts = await db.Appointments
            //        .AsNoTracking()
            //        .Where(a =>
            //            a.Patient.FirstName.Contains(q)||
            //            a.Patient.LastName.Contains(q))
            //        .Select(a => new SearchSuggestionDto
            //        {
            //            Id=a.Id.ToString(),
            //            Type=SearchEntityType.Appointment,
            //            DisplayText=a.Patient.FirstName+" "+a.Patient.LastName,
            //            Score=60
            //        })
            //        .ToListAsync(ct);

            //    results.AddRange(appts);
            //}


        }
    }
}