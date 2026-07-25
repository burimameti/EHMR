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

                // ---- Rule: therapy cycle past its end date, still open ----
                // TODO: adjust TherapyCycleStatus/EndDate to your actual property names —
                // I don't have TherapyCycle.cs, this is a placeholder shape.
                // var overdueCycles = await db.TherapyCycles
                //     .Where(t => t.Status != TherapyCycleStatus.Completed)
                //     .Where(t => t.EndDate < DateTime.UtcNow)
                //     .Select(t => new { t.Id, t.PatientId })
                //     .ToListAsync();
                //
                // foreach (var cycle in overdueCycles)
                // {
                //     var key = $"OverdueCycle:{cycle.Id}";
                //     if (existing.Contains(key)) continue;
                //
                //     newAlerts.Add(new Alert
                //     {
                //         PatientId = cycle.PatientId,
                //         Level = AlertLevel.Critical,
                //         Message = "Терапевтскиот циклус го поминал очекуваниот крај.",
                //         DedupKey = key
                //     });
                // }

                if(newAlerts.Count>0)
                {
                    db.Alerts.AddRange(newAlerts);
                    await db.SaveChangesAsync();
                }

                return newAlerts.Count;
            }
            catch(Exception ex )
            {

                throw new Exception($"ERROR OCCURED ON ALERT : Service {ex.ToString()}", ex);
            }
           
        }
    }
}
