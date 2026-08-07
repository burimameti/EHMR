using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Infrastructure.Persistence.Configs;

public class TherapyProtocolSeeder : IEntitySeeder
{
    public int Order => 110;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.TherapyProtocols.AnyAsync(ct))
            return;
        try
        {
            var baseDate = new DateTime(2026, 5, 5);
            context.Set<TherapyProtocol>().AddRange(

                  new TherapyProtocol
                  {
                      Id=Guid.Parse("11111111-1111-1111-1111-111111111111"),
                      Name="Ревматоиден Артритис - DMARD Терапија",

                      DiseaseCategory="Ревматологија",
                      Description="Следење на активност на болест, лабораториски параметри и ефект од метотрексат или други DMARD лекови.",
                      CreatedByDoctor="Д-р Ревматолог",
                      DurationInDays=365,
                      CreatedAt=baseDate.AddDays(-60)
                  },

                  new TherapyProtocol
                  {
                      Id=Guid.Parse("C3333333-3333-3333-3333-333333333333"),
                      Name="Анкилозирачки Спондилитис",
                      DiseaseCategory="Ревматологија",
                      Description="Следење на воспалителна активност, физикална рехабилитација и биолошка терапија.",
                      CreatedByDoctor="Д-р Ревматолог",
                      DurationInDays=365,
                      CreatedAt=baseDate.AddDays(-40)
                  },

                  new TherapyProtocol
                  {
                      Id=Guid.Parse("D4444444-4444-4444-4444-444444444444"),

                      Name="Псоријатичен Артритис",
                      DiseaseCategory="Ревматологија",
                      Description="Следење на зглобни симптоми, кожни промени и одговор на имуно-модулаторна терапија.",
                      CreatedByDoctor="Д-р Ревматолог",
                      DurationInDays=365,
                      CreatedAt=baseDate.AddDays(-35)
                  },

                  new TherapyProtocol
                  {
                      Id=Guid.Parse("E5555555-5555-5555-5555-555555555555"),
                      Name="Системски Лупус Еритематозус",

                      DiseaseCategory="Ревматологија",
                      Description="Редовно следење на лабораториски параметри, органски манифестации и имуносупресивна терапија.",
                      CreatedByDoctor="Д-р Ревматолог",
                      DurationInDays=365,
                      CreatedAt=baseDate.AddDays(-30)
                  },

                  new TherapyProtocol
                  {
                      Id=Guid.Parse("F6666666-6666-6666-6666-666666666666"),
                      Name="Остеопороза",

                      DiseaseCategory="Ревматологија",
                      Description="Следење на коскена густина, витамин Д, калциум и превенција на фрактури.",
                      CreatedByDoctor="Д-р Ревматолог",
                      DurationInDays=365,
                      CreatedAt=baseDate.AddDays(-25)
                  },

                  new TherapyProtocol
                  {
                      Id=Guid.Parse("B7777777-7777-7777-7777-777777777777"),
                      Name="Фибромијалгија",

                      DiseaseCategory="Ревматологија",
                      Description="Контрола на хронична болка, физичка активност, сон и мултидисциплинарен пристап.",
                      CreatedByDoctor="Д-р Ревматолог",
                      DurationInDays=180,
                      CreatedAt=baseDate.AddDays(-20)
                  },

                  new TherapyProtocol
                  {
                      Id=Guid.Parse("A8888888-8888-8888-8888-888888888888"),
                      Name="Гихт (Подагра)",

                      DiseaseCategory="Ревматологија",
                      Description="Контрола на урична киселина, исхрана и долгорочна превенција на напади.",
                      CreatedByDoctor="Д-р Ревматолог",
                      DurationInDays=180,
                      CreatedAt=baseDate.AddDays(-15)
                  },

                  new TherapyProtocol
                  {
                      Id=Guid.Parse("C9999999-9999-9999-9999-999999999999"),
                      Name="Сјогренов Синдром",

                      DiseaseCategory="Ревматологија",
                      Description="Следење на сувост на очи и уста, автоимуни параметри и системски компликации.",
                      CreatedByDoctor="Д-р Ревматолог",
                      DurationInDays=365,
                      CreatedAt=baseDate.AddDays(-10)
                  },

                  new TherapyProtocol
                  {
                      Id=Guid.Parse("D1010101-1010-1010-1010-101010101010"),
                      Name="Васкулитис - Долгорочно Следење",

                      DiseaseCategory="Ревматологија",
                      Description="Следење на воспалителни маркери, органско зафаќање и имуносупресивна терапија.",
                      CreatedByDoctor="Д-р Ревматолог",
                      DurationInDays=365,
                      CreatedAt=baseDate.AddDays(-5)
                  });

            await context.SaveChangesAsync();
        }
        catch(Exception ex)
        {
            throw new Exception("", ex);
        }
    }
}