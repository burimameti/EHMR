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
                DoctorName=users[0].FirstName,
                DoctorSurname=users[0].LastName,
                // LicenseNumber="MK-LIC-0001",
                //  Specialty="Cardiology",
                ContactPhone="+38970111111",
                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor2,
                UserId=users[1].Id,
                DoctorName=users[1].FirstName, DoctorSurname =users[1].LastName,
                //  LicenseNumber="MK-LIC-0002",
                //  Specialty="Neurology",
                ContactPhone="+38970111112",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor3,
                UserId=users[2].Id, DoctorName = users[2].FirstName, DoctorSurname =users[2].LastName,

                //LicenseNumber="MK-LIC-0003",
                // Specialty="Pediatrics",
                ContactPhone="+38970111113",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor4,
                UserId=users[3].Id,
                DoctorName=users[3].FirstName, DoctorSurname =users[3].LastName,

                ContactPhone="+38970111114",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor5,
                UserId=users[4].Id,
                DoctorName=users[4].FirstName, DoctorSurname =users[4].LastName,

                ContactPhone="+38970111115",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor6,
                UserId=users[5].Id,
                DoctorName =users[5].FirstName, DoctorSurname =users[5].LastName,

                ContactPhone="+38970111116",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor7,
                UserId=users[6].Id,

               DoctorName =users[6].FirstName, DoctorSurname =users[6].LastName,
                ContactPhone="+38970111117",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor8,
                UserId=users[7].Id,
                DoctorName =users[7].FirstName, DoctorSurname =users[7].LastName,

                ContactPhone="+38970111118",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor9,
                UserId=users[8].Id,
                DoctorName =users[8].FirstName, DoctorSurname =users[8].LastName,

                ContactPhone="+38970111119",

                IsActive=true
            },


            new Doctor
            {
                Id=SeedIds.Doctor10,
                UserId=users[9].Id,
                DoctorName =users[9].FirstName, DoctorSurname =users[9].LastName,

                ContactPhone="+38970111120",

                IsActive=true
            }

        );



        await context.SaveChangesAsync(ct);
    }
}