using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    // =====================================================
    // 5. EXECUTION LAYER
    // =====================================================

    public class TherapyCycleSeeder : IEntitySeeder
    {
        public int Order => 40;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.TherapyCycles.AnyAsync(ct))
                return;

            await context.Set<TherapyCycle>().AddRangeAsync(new TherapyCycle
            {
                Id=SeedIds.Cycle1,

                TherapyScheduleId=SeedIds.Schedule1,
                CycleNumber=1,
                // Нормален тековен план
                PlannedStartDate=DateTime.UtcNow,
                PlannedEndDate=DateTime.UtcNow.AddDays(21),
                Status=TherapyStatus.Planned,
                IsEscalatedToMissedGroup=false,
                ReasonForMissing=null,
                StatusChangedAt=null
            },
            new TherapyCycle
            {
                Id=SeedIds.Cycle2,

                TherapyScheduleId=SeedIds.Schedule2,
                CycleNumber=2,
                // Нормален иден план
                PlannedStartDate=DateTime.UtcNow.AddDays(22),
                PlannedEndDate=DateTime.UtcNow.AddDays(52),
                Status=TherapyStatus.Planned,
                IsEscalatedToMissedGroup=false,
                ReasonForMissing=null,
                StatusChangedAt=null
            },
            // ТЕСТ ПОДАТОК 1: Пациент што пропуштил вакцина/терапија минатиот месец
            new TherapyCycle
            {
                Id=SeedIds.Cycle3,

                TherapyScheduleId=SeedIds.Schedule2,
                CycleNumber=3,
                // Датуми намерно ставени во минатото за да го фати филтерот
                PlannedStartDate=DateTime.UtcNow.AddMonths(-1).AddDays(-10),
                PlannedEndDate=DateTime.UtcNow.AddMonths(-1),
                Status=TherapyStatus.Missed,
                IsEscalatedToMissedGroup=true,
                StatusChangedAt=DateTime.UtcNow.AddMonths(-1).AddDays(3), // Ескалирано по 3 дена
                ReasonForMissing="Пациентот пријави силна алергиска реакција и осип по примањето на претходната доза."
            },
            // ТЕСТ ПОДАТОК 2: Пациент кој пропуштил терапија, но уште нема медицинско образложение
            new TherapyCycle
            {
                Id=SeedIds.Cycle4,

                TherapyScheduleId=SeedIds.Schedule2,
                CycleNumber=4,
                PlannedStartDate=DateTime.UtcNow.AddDays(-15),
                PlannedEndDate=DateTime.UtcNow.AddDays(-5), // Поминат рок пред 5 дена (Бекграунд процесот веќе го фатил)
                Status=TherapyStatus.Missed,
                IsEscalatedToMissedGroup=true,
                StatusChangedAt=DateTime.UtcNow.AddDays(-2),
                ReasonForMissing="Системска нотификација: Потребно е внесување причина од медицинско лице."
            });

            // IMPORTANT: save once per seeder (recommended)
            await context.SaveChangesAsync();
        }
    }
}