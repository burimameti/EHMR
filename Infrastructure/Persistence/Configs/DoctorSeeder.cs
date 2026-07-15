using EHMR.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;


namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class DoctorSeeder : IEntitySeeder
{
    public int Order => 4;


    public async Task SeedAsync(
        DesktopTherapyDbContext context,
        CancellationToken ct = default)
    {
        if(await context.Doctors.AnyAsync(ct))
            return;



        var users = await context.Users
            .OrderBy(x => x.FirstName)
            .Take(15)
            .ToListAsync(ct);



        if(users.Count<10)
            return;



        await context.Doctors.AddRangeAsync(
            new Doctor
            {
                Id=SeedIds.Doctor1,
                UserId=users[0].Id,

                LicenseNumber="MK-LIC-0001",
                Specialty="Cardiology",
                ContactPhone="+38970111111",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor2,
                UserId=users[1].Id,

                LicenseNumber="MK-LIC-0002",
                Specialty="Neurology",
                ContactPhone="+38970111112",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor3,
                UserId=users[2].Id,

                LicenseNumber="MK-LIC-0003",
                Specialty="Pediatrics",
                ContactPhone="+38970111113",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor4,
                UserId=users[3].Id,

                LicenseNumber="MK-LIC-0004",
                Specialty="Orthopedics",
                ContactPhone="+38970111114",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor5,
                UserId=users[4].Id,

                LicenseNumber="MK-LIC-0005",
                Specialty="Dermatology",
                ContactPhone="+38970111115",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor6,
                UserId=users[5].Id,

                LicenseNumber="MK-LIC-0006",
                Specialty="General Surgery",
                ContactPhone="+38970111116",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor7,
                UserId=users[6].Id,

                LicenseNumber="MK-LIC-0007",
                Specialty="Internal Medicine",
                ContactPhone="+38970111117",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor8,
                UserId=users[7].Id,

                LicenseNumber="MK-LIC-0008",
                Specialty="Gynecology",
                ContactPhone="+38970111118",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor9,
                UserId=users[8].Id,

                LicenseNumber="MK-LIC-0009",
                Specialty="Ophthalmology",
                ContactPhone="+38970111119",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor10,
                UserId=users[9].Id,

                LicenseNumber="MK-LIC-0010",
                Specialty="Psychiatry",
                ContactPhone="+38970111120",

                IsActive=true
            }

        );



        await context.SaveChangesAsync(ct);
    }
}