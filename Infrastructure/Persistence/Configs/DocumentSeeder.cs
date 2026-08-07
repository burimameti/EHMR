using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class DocumentSeeder : IEntitySeeder
{
    public int Order => 66;

    public async Task SeedAsync(
        DesktopTherapyDbContext context,
        CancellationToken ct = default)
    {
        if(await context.PatientDocuments.AnyAsync(ct))
            return;

        await context.PatientDocuments.AddAsync(new PatientDocument
        {
            Id=Guid.NewGuid(),

            PatientId=SeedIds.Patient1,
            EncounterId=SeedIds.Encounter1,

            Title="Иницијален ревматолошки извештај",
            Description="Првичен специјалистички преглед.",

            FileName="initial_report.pdf",
            StoredPath="/documents/initial_report.pdf",
            ContentType="application/pdf",

           // FileSizeInBytes=215_420,

            UploadedAt=DateTime.UtcNow,
            IsCritical=true,
            IsDeleted=false
        }, ct);

        await context.SaveChangesAsync(ct);
    }
}