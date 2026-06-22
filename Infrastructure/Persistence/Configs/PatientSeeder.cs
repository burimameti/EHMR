using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Seeders;

public class PatientSeeder : IEntitySeeder
{
    int IEntitySeeder.Order => 8;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Patients.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;

        var patients = new List<Patient>
        {new()
            {
                Id = SeedIds.Patient1,
                FirstName = "Jeton",
                LastName = "Jetoni",
                Gender = Gender.Male,
                City = "Skopje",
                Phone = "+38970111222",
                Email = "j.j@email.com",
                BloodType = "A+",
                Allergies = "None",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-30),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient2,
                FirstName = "Test",
                LastName = "Test",
                Gender = Gender.Male,
                City = "Skopje",
                Phone = "+38970111222",
                Email = "t.t@email.com",
                BloodType = "A+",
                Allergies = "None",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-30),
                IsDeleted = false
            },

            new()
            {
                Id = SeedIds.Patient3,
                FirstName = "Asdren",
                LastName = "Shabani",
                Gender = Gender.Female,
                City = "Struga",
                BloodType = "B-",
                Allergies = "Penicillin",
                Status = PatientStatus.Chronic,
                RegistrationDate = now.AddDays(-28),
                IsDeleted = false
            },

            new()
            {
                Id = SeedIds.Patient4,
                FirstName = "Muharem",
                LastName = "Muharemi",
                Gender = Gender.Male,
                City = "Gostivar",
                BloodType = "O+",
                Allergies = "Dust",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-26),
                IsDeleted = false
            },

            new() { Id = SeedIds.Patient5, FirstName="Tomi", LastName="Tom", City="Skopje", Gender=Gender.Female, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-24), IsDeleted=false },
            new() { Id = SeedIds.Patient6, FirstName="Vujce", LastName="Tujce", City="Kumanovo", Gender=Gender.Male, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-22), IsDeleted=false },
            new() { Id = SeedIds.Patient7, FirstName="Mome", LastName="Tome", City="Ohrid", Gender=Gender.Female, Status=PatientStatus.Chronic, RegistrationDate=now.AddDays(-20), IsDeleted=false },
            new() { Id = SeedIds.Patient8, FirstName="Tome", LastName="Lome", City="Tetovo", Gender=Gender.Male, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-18), IsDeleted=false },
            new()
            {
                Id = SeedIds.Patient9, FirstName="Besa", LastName="Tezja", City="Veles", Gender=Gender.Female, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-16), IsDeleted=false
            },
            new()
            {
                Id = SeedIds.Patient10, FirstName="Imran", LastName="Ma", City="Struga", Gender=Gender.Male, Status=PatientStatus.Inactive, RegistrationDate=now.AddDays(-14), IsDeleted=false
            },

            new()
            {
                Id = SeedIds.Patient11, FirstName="Sara", LastName="Sara", City="Skopje", Gender=Gender.Female, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-12), IsDeleted=false
            },
            new()
            {
                Id = SeedIds.Patient12, FirstName="Halla", LastName="E Gjalit Menjxhes", City="Bitola", Gender=Gender.Male, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-10), IsDeleted=false
            },
            new()
            { Id = SeedIds.Patient13, FirstName="Ana", LastName="Ana", City="Prilep", Gender=Gender.Female, Status=PatientStatus.Chronic, RegistrationDate=now.AddDays(-9), IsDeleted=false
            },
            new()
            { Id = SeedIds.Patient14, FirstName="Gore", LastName="Dole", City="Skopje", Gender=Gender.Male, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-8), IsDeleted=false },
            new() { Id = SeedIds.Patient15, FirstName="Mare", LastName="Nare", City="Ohrid", Gender=Gender.Female, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-7), IsDeleted=false },

            new() { Id = SeedIds.Patient16, FirstName="Dare", LastName="Mare", City="Tetovo", Gender=Gender.Male, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-6), IsDeleted=false },
            new() { Id = SeedIds.Patient17, FirstName="Ivana", LastName="Stariot", City="Kavadarci", Gender=Gender.Female, Status=PatientStatus.Chronic, RegistrationDate=now.AddDays(-5), IsDeleted=false },
            new() { Id = SeedIds.Patient18, FirstName="Bojan", LastName="Mladiot", City="Skopje", Gender=Gender.Male, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-4), IsDeleted=false },
            new() { Id = SeedIds.Patient19, FirstName="Eli", LastName="Teli", City="Bitola", Gender=Gender.Female, Status=PatientStatus.Inactive, RegistrationDate=now.AddDays(-3), IsDeleted=false },
            new() { Id = SeedIds.Patient20, FirstName="Deli", LastName="Leli", City="Skopje", Gender=Gender.Female, Status=PatientStatus.Active, RegistrationDate=now.AddDays(-2), IsDeleted=false }
        };

        await context.Patients.AddRangeAsync(patients, ct);
        await context.SaveChangesAsync(ct);
    }
}