using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public class DoctorSeeder : IEntitySeeder
{
    public int Order => 4;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Set<Doctor>().AnyAsync(ct))
            return;

        var doctors = new List<Doctor>
        {
            new()
            {
                Id = SeedIds.Doctor1,
                UserId = SeedIds.DocUser1,
                Specialty = "Онкологија",
                LicenseNumber = "MK-ONC-001",
                ContactPhone = "+38970000001",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Doctor2,
                UserId = SeedIds.DocUser2,
                Specialty = "Ортопедија",
                LicenseNumber = "MK-ORT-002",
                ContactPhone = "+38970000002",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Doctor3,
                UserId = SeedIds.DocUser3,
                Specialty = "Неврологија",
                LicenseNumber = "MK-NEU-003",
                ContactPhone = "+38970000003",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Doctor4,
                UserId = SeedIds.DocUser4,
                Specialty = "Физикална терапија",
                LicenseNumber = "MK-PHY-004",
                ContactPhone = "+38970000004",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Doctor5,
                UserId = SeedIds.DocUser5,
                Specialty = "Кардиологија",
                LicenseNumber = "MK-CAR-005",
                ContactPhone = "+38970000005",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Doctor6,
                UserId = SeedIds.DocUser6,
                Specialty = "Дерматологија",
                LicenseNumber = "MK-DER-006",
                ContactPhone = "+38970000006",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Doctor7,
                UserId = SeedIds.DocUser7,
                Specialty = "ОРЛ",
                LicenseNumber = "MK-ENT-007",
                ContactPhone = "+38970000007",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Doctor8,
                UserId = SeedIds.DocUser8,
                Specialty = "Психијатрија",
                LicenseNumber = "MK-PSY-008",
                ContactPhone = "+38970000008",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Doctor9,
                UserId = SeedIds.DocUser9,
                Specialty = "Генерална медицина",
                LicenseNumber = "MK-GEN-009",
                ContactPhone = "+38970000009",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Doctor10,
                UserId = SeedIds.DocUser10,
                Specialty = "Ендокринологија",
                LicenseNumber = "MK-END-010",
                ContactPhone = "+38970000010",
                IsActive = true
            }
        };

        await context.Set<Doctor>().AddRangeAsync(doctors, ct);
        await context.SaveChangesAsync(ct);
    }
}