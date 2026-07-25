using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class TherapyCycleSeeder : IEntitySeeder
    {
        public int Order => 40;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.TherapyCycles.AnyAsync(ct))
                return;

            var patients = await context.Patients
                .OrderBy(p => p.FirstName)
                .Take(10)
                .ToListAsync(ct);

            if(patients.Count<5)
                throw new InvalidOperationException("Not enough patients for TherapyCycleSeeder");

            var now = DateTime.UtcNow;

            var cycles = new List<TherapyCycle>
            {
                // ================= ACTIVE / PLANNED =================

                new TherapyCycle
                {
                    Id = SeedIds.Cycle1,
                    PatientId = patients[0].Id,
                    TherapyCyleNumber = "1",
                    Status = TherapyStatus.Planned,
                    StartDate = now.AddDays(2),
                    Notes = "Планиран почеток на физиотерапија за лумбална болка"
                },

                new TherapyCycle
                {
                    Id = SeedIds.Cycle2,
                    PatientId = patients[1].Id,
                    TherapyCyleNumber = "1",
                    Status = TherapyStatus.Active,
                    StartDate = now.AddDays(-10),
                    Notes = "Активна терапија за колено - напредок стабилен"
                },

                new TherapyCycle
                {
                    Id = SeedIds.Cycle3,
                    PatientId = patients[2].Id,
                    TherapyCyleNumber = "1",
                    Status = TherapyStatus.Completed,
                    StartDate = now.AddDays(-30),
                    EndDate = now.AddDays(-5),
                    Notes = "Завршена терапија за цервикална болка"
                },

                new TherapyCycle
                {
                    Id = SeedIds.Cycle4,
                    PatientId = patients[3].Id,
                    TherapyCyleNumber = "1",
                    Status = TherapyStatus.Missed,
                    StartDate = now.AddDays(-7),
                    Notes = "Пациентот не се појави на повеќе сесии"
                },

                new TherapyCycle
                {
                    Id = SeedIds.Cycle5,
                    PatientId = patients[4].Id,
                    TherapyCyleNumber = "1",
                    Status = TherapyStatus.Suspended,
                    StartDate = now.AddDays(-3),
                    Notes = "Терапијата откажана по барање на пациент"
                },

                // ================= MORE REALISTIC CYCLES =================

                new TherapyCycle
                {
                    Id = SeedIds.Cycle6,
                    PatientId = patients[5].Id,
                    TherapyCyleNumber = "2",
                    Status = TherapyStatus.Active,
                    StartDate = now.AddDays(-15),
                    Notes = "Втора рунда терапија за хронична болка"
                },

                new TherapyCycle
                {
                    Id = SeedIds.Cycle7,
                    PatientId = patients[6].Id,
                    TherapyCyleNumber = "1",
                    Status = TherapyStatus.Planned,
                    StartDate = now.AddDays(5),
                    Notes = "Планирана рехабилитација после повреда"
                },

                new TherapyCycle
                {
                    Id = SeedIds.Cycle8,
                    PatientId = patients[7].Id,
                    TherapyCyleNumber = "1",
                    Status = TherapyStatus.Completed,
                    StartDate = now.AddDays(-40),
                    EndDate = now.AddDays(-20),
                    Notes = "Успешно завршена пост-оперативна рехабилитација"
                },

                new TherapyCycle
                {
                    Id = SeedIds.Cycle9,
                    PatientId = patients[8].Id,
                    TherapyCyleNumber = "1",
                    Status = TherapyStatus.Active,
                    StartDate = now.AddDays(-12),
                    Notes = "Психосоматска терапија во тек"
                },

                new TherapyCycle
                {
                    Id = SeedIds.Cycle10,
                    PatientId = patients[9].Id,
                    TherapyCyleNumber = "1",
                    Status = TherapyStatus.Missed,
                    StartDate = now.AddDays(-8),
                    Notes = "Пациентот пропушти повеќе термини"
                },

                // ================= EXTRA EDGE CASE =================

                new TherapyCycle
                {
                    Id = SeedIds.Cycle11,
                    PatientId = patients[0].Id,
                    TherapyCyleNumber = "2",
                    Status = TherapyStatus.Planned,
                    StartDate = now.AddDays(10),
                    Notes = "Втор циклус - продолжена терапија"
                }
            };

            await context.TherapyCycles.AddRangeAsync(cycles, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}