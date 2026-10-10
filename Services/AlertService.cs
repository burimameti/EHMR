using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    public interface IAlertService
    {
        /// <returns>How many new alerts were inserted.</returns>
        Task<int> RunDailySweepAsync();
    }

    public class AlertService : IAlertService
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

        public AlertService(IDbContextFactory<DesktopTherapyDbContext> factory) => _factory=factory;

        public async Task<int> RunDailySweepAsync()
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                // Only unresolved alerts count as "already open" — a resolved one
                // for the same condition should be free to re-fire if it recurs.
                var existingKeys = await db.Alerts
                    .Where(a => !a.IsResolved)
                    .Select(a => a.DedupKey)
                    .ToListAsync();
                var existing = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

                var newAlerts = new List<Alert>();

                // ---- Rule: no completed visit in 90+ days ----
                var staleCutoff = DateTime.UtcNow.AddDays(-90);
                var stalePatientIds = await db.Patients
                    .Where(p => p.Status==PatientStatus.Active)
                    .Where(p => !p.Appointments.Any(a =>
                        a.Status==AppointmentStatus.Completed&&
                        a.ScheduledStart>staleCutoff))
                    .Select(p => p.Id)
                    .ToListAsync();

                foreach(var patientId in stalePatientIds)
                {
                    var key = $"StaleVisit:{patientId}";
                    if(existing.Contains(key)) continue;

                    newAlerts.Add(new Alert
                    {
                        PatientId=patientId,
                        Level=AlertLevel.Warning,
                        Message="Пациентот нема посета подолго од 90 дена.",
                        DedupKey=key
                    });
                }

                if(newAlerts.Count>0)
                {
                    db.Alerts.AddRange(newAlerts);
                    await db.SaveChangesAsync();
                }

                return newAlerts.Count;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AlertService] Daily sweep failed: {ex}");
                throw;
            }
           
        }
    }
}
