using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Seeders;

public class PatientSeeder : IEntitySeeder
{
    int IEntitySeeder.Order => 8; // must run after DoctorSeeder and MedicineSeeder (Order 5)

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var medicineIds = new[]
        {
            SeedIds.Med1, SeedIds.Med2, SeedIds.Med3, SeedIds.Med4, SeedIds.Med5,
            SeedIds.Med6, SeedIds.Med7, SeedIds.Med8, SeedIds.Med9, SeedIds.Med10
        };

        var patients = new List<Patient>
        {
            new()
            {
                Id = SeedIds.Patient1,
                DoctorId = SeedIds.Doctor1,
                FirstName = "Јетон",
                LastName = "Исмаили",
                NationalId = "0101990123456",
                SzboNumber = "SZBO-000001",
                BirthDate = new DateTime(1990, 1, 15),
                Gender = Gender.Male,
                City = "Skopje",
                Address = "ул. Илинденска 12",
                PostalCode = "1000",
                Phone = "+38970111222",
                Email = "jeton.ismaili@email.com",
                EmergencyContactName = "Фатиме Исмаили",
                EmergencyContactPhone = "+38970111223",
                EmergencyRelationship = "Сопруга",
                BloodType = "A+",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-30),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient2,
                DoctorId = SeedIds.Doctor2,
                FirstName = "Елена",
                LastName = "Петровска",
                NationalId = "1503988654321",
                SzboNumber = "SZBO-000002",
                BirthDate = new DateTime(1988, 3, 15),
                Gender = Gender.Female,
                City = "Skopje",
                Address = "бул. Партизански одреди 45",
                PostalCode = "1000",
                Phone = "+38970222333",
                Email = "elena.petrovska@email.com",
                EmergencyContactName = "Марко Петровски",
                EmergencyContactPhone = "+38970222334",
                EmergencyRelationship = "Брат",
                BloodType = "A+",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-30),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient3,
                DoctorId = SeedIds.Doctor3,
                FirstName = "Арјета",
                LastName = "Шабани",
                NationalId = "2207985112233",
                SzboNumber = "SZBO-000003",
                BirthDate = new DateTime(1985, 7, 22),
                Gender = Gender.Female,
                City = "Struga",
                Address = "ул. Кеј Македонија 8",
                PostalCode = "6330",
                Phone = "+38975333444",
                Email = "arjeta.shabani@email.com",
                EmergencyContactName = "Дритон Шабани",
                EmergencyContactPhone = "+38975333445",
                EmergencyRelationship = "Сопруг",
                BloodType = "B-",
                Allergies = "Пеницилин",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-28),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient4,
                DoctorId = SeedIds.Doctor4,
                FirstName = "Мухарем",
                LastName = "Реџепи",
                NationalId = "0512982445566",
                SzboNumber = "SZBO-000004",
                BirthDate = new DateTime(1982, 12, 5),
                Gender = Gender.Male,
                City = "Gostivar",
                Address = "ул. Младинска 3",
                PostalCode = "1230",
                Phone = "+38970444555",
                Email = "muharem.rexhepi@email.com",
                EmergencyContactName = "Сара Реџепи",
                EmergencyContactPhone = "+38970444556",
                EmergencyRelationship = "Сопруга",
                BloodType = "O+",
                Allergies = "Прашина",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-26),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient5,
                DoctorId = SeedIds.Doctor5,
                FirstName = "Томислав",
                LastName = "Николовски",
                NationalId = "1809991778899",
                SzboNumber = "SZBO-000005",
                BirthDate = new DateTime(1991, 9, 18),
                Gender = Gender.Male,
                City = "Skopje",
                Address = "ул. 11 Октомври 21",
                PostalCode = "1000",
                Phone = "+38970555666",
                Email = "tomislav.nikolovski@email.com",
                EmergencyContactName = "Ана Николовска",
                EmergencyContactPhone = "+38970555667",
                EmergencyRelationship = "Сестра",
                BloodType = "AB+",
                Allergies = "Нема",
                Status = PatientStatus.Inactive,
                InactiveReason = "Демо пациент — неактивен.",
                RegistrationDate = now.AddDays(-24),
                IsDeleted = false
            },

        };

        // Keep the demo patient catalogue limited to the five explicitly seeded patients.
        // Historical seed patients 6–20 are removed by the dedicated demo cleanup seeder.
        // Idempotent expansion: add missing seeded patients without duplicating records.
        var existingPatientIds = await context.Patients
            .Where(x => patients.Select(p => p.Id).Contains(x.Id))
            .Select(x => x.Id)
            .ToHashSetAsync(ct);

        var existingPatients = await context.Patients
            .Where(x => patients.Select(p => p.Id).Contains(x.Id))
            .ToListAsync(ct);

        var missingPatients = patients
            .Where(x => !existingPatientIds.Contains(x.Id))
            .ToList();

        if(missingPatients.Count > 0)
            await context.Patients.AddRangeAsync(missingPatients, ct);

        // These fixed IDs are demo-only records; refresh their seed fields so an older
        // database also marks the fifth patient inactive.
        foreach(var existing in existingPatients)
        {
            var seed = patients.First(x => x.Id == existing.Id);
            existing.DoctorId = seed.DoctorId;
            existing.FirstName = seed.FirstName;
            existing.LastName = seed.LastName;
            existing.NationalId = seed.NationalId;
            existing.SzboNumber = seed.SzboNumber;
            existing.BirthDate = seed.BirthDate;
            existing.Gender = seed.Gender;
            existing.City = seed.City;
            existing.Address = seed.Address;
            existing.PostalCode = seed.PostalCode;
            existing.Phone = seed.Phone;
            existing.Email = seed.Email;
            existing.EmergencyContactName = seed.EmergencyContactName;
            existing.EmergencyContactPhone = seed.EmergencyContactPhone;
            existing.EmergencyRelationship = seed.EmergencyRelationship;
            existing.BloodType = seed.BloodType;
            existing.Allergies = seed.Allergies;
            existing.Status = seed.Status;
            existing.InactiveReason = seed.InactiveReason;
            existing.IsDeleted = false;
        }

        await context.SaveChangesAsync(ct);
    }
}