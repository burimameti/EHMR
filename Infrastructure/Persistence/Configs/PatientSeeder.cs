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

        },

            new()
            {
                Id = SeedIds.Patient6, DoctorId = SeedIds.Doctor6,
                FirstName = "Александар", LastName = "Стојанов",
                NationalId = "1204900123457", SzboNumber = "SZBO-000006",
                BirthDate = new DateTime(1990, 4, 12), Gender = Gender.Male,
                City = "Skopje", Address = "ул. Водњанска 14", PostalCode = "1000",
                Phone = "+38970666006", Email = "aleksandar.stojanov@email.com",
                EmergencyContactName = "Марија Стојанова", EmergencyContactPhone = "+38970666016",
                EmergencyRelationship = "Сопруга", BloodType = "A+", Allergies = "Нема",
                Status = PatientStatus.Active, RegistrationDate = now.AddDays(-22), IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient7, DoctorId = SeedIds.Doctor7,
                FirstName = "Марија", LastName = "Јованова",
                NationalId = "0206920123458", SzboNumber = "SZBO-000007",
                BirthDate = new DateTime(1992, 6, 2), Gender = Gender.Female,
                City = "Bitola", Address = "ул. Широк Сокак 18", PostalCode = "7000",
                Phone = "+38970666007", Email = "marija.jovanova@email.com",
                EmergencyContactName = "Петар Јованов", EmergencyContactPhone = "+38970666017",
                EmergencyRelationship = "Сопруг", BloodType = "O+", Allergies = "Нема",
                Status = PatientStatus.Active, RegistrationDate = now.AddDays(-21), IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient8, DoctorId = SeedIds.Doctor8,
                FirstName = "Бесник", LastName = "Адеми",
                NationalId = "1107880123459", SzboNumber = "SZBO-000008",
                BirthDate = new DateTime(1988, 7, 11), Gender = Gender.Male,
                City = "Tetovo", Address = "ул. Илинденска 27", PostalCode = "1200",
                Phone = "+38970666008", Email = "besnik.ademi@email.com",
                EmergencyContactName = "Лиридона Адеми", EmergencyContactPhone = "+38970666018",
                EmergencyRelationship = "Сопруга", BloodType = "B+", Allergies = "Нема",
                Status = PatientStatus.Active, RegistrationDate = now.AddDays(-20), IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient9, DoctorId = SeedIds.Doctor9,
                FirstName = "Ивана", LastName = "Костова",
                NationalId = "2509950123460", SzboNumber = "SZBO-000009",
                BirthDate = new DateTime(1995, 9, 25), Gender = Gender.Female,
                City = "Ohrid", Address = "ул. Туристичка 9", PostalCode = "6000",
                Phone = "+38970666009", Email = "ivana.kostova@email.com",
                EmergencyContactName = "Горан Костов", EmergencyContactPhone = "+38970666019",
                EmergencyRelationship = "Татко", BloodType = "A-", Allergies = "Нема",
                Status = PatientStatus.Active, RegistrationDate = now.AddDays(-19), IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient10, DoctorId = SeedIds.Doctor10,
                FirstName = "Арбен", LastName = "Селими",
                NationalId = "0801830123461", SzboNumber = "SZBO-000010",
                BirthDate = new DateTime(1983, 1, 8), Gender = Gender.Male,
                City = "Kumanovo", Address = "ул. Октомвриска 6", PostalCode = "1300",
                Phone = "+38970666010", Email = "arben.selimi@email.com",
                EmergencyContactName = "Вјолца Селими", EmergencyContactPhone = "+38970666020",
                EmergencyRelationship = "Сопруга", BloodType = "O-", Allergies = "Нема",
                Status = PatientStatus.Active, RegistrationDate = now.AddDays(-18), IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient11, DoctorId = SeedIds.Doctor1,
                FirstName = "Сања", LastName = "Митревска",
                NationalId = "1402900123462", SzboNumber = "SZBO-000011",
                BirthDate = new DateTime(1990, 2, 14), Gender = Gender.Female,
                City = "Skopje", Address = "ул. Дебарца 11", PostalCode = "1000",
                Phone = "+38970666011", Email = "sanja.mitrevska@email.com",
                EmergencyContactName = "Владимир Митревски", EmergencyContactPhone = "+38970666021",
                EmergencyRelationship = "Сопруг", BloodType = "AB+", Allergies = "Нема",
                Status = PatientStatus.Active, RegistrationDate = now.AddDays(-17), IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient12, DoctorId = SeedIds.Doctor2,
                FirstName = "Фисник", LastName = "Красниќи",
                NationalId = "0907860123463", SzboNumber = "SZBO-000012",
                BirthDate = new DateTime(1986, 7, 9), Gender = Gender.Male,
                City = "Gostivar", Address = "ул. Борче Јовановски 5", PostalCode = "1230",
                Phone = "+38970666012", Email = "fisnik.krasniqi@email.com",
                EmergencyContactName = "Мира Красниќи", EmergencyContactPhone = "+38970666022",
                EmergencyRelationship = "Сопруга", BloodType = "B-", Allergies = "Нема",
                Status = PatientStatus.Active, RegistrationDate = now.AddDays(-16), IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient13, DoctorId = SeedIds.Doctor3,
                FirstName = "Кристина", LastName = "Трајковска",
                NationalId = "1709910123464", SzboNumber = "SZBO-000013",
                BirthDate = new DateTime(1991, 9, 17), Gender = Gender.Female,
                City = "Prilep", Address = "ул. Маршал Тито 23", PostalCode = "7500",
                Phone = "+38970666013", Email = "kristina.trajkovska@email.com",
                EmergencyContactName = "Никола Трајковски", EmergencyContactPhone = "+38970666023",
                EmergencyRelationship = "Сопруг", BloodType = "A+", Allergies = "Нема",
                Status = PatientStatus.Active, RegistrationDate = now.AddDays(-15), IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient14, DoctorId = SeedIds.Doctor4,
                FirstName = "Леарт", LastName = "Ибраими",
                NationalId = "0605840123465", SzboNumber = "SZBO-000014",
                BirthDate = new DateTime(1984, 5, 6), Gender = Gender.Male,
                City = "Strumica", Address = "ул. Ленинова 17", PostalCode = "2400",
                Phone = "+38970666014", Email = "leart.ibraimi@email.com",
                EmergencyContactName = "Елмира Ибраими", EmergencyContactPhone = "+38970666024",
                EmergencyRelationship = "Сопруга", BloodType = "O+", Allergies = "Нема",
                Status = PatientStatus.Active, RegistrationDate = now.AddDays(-14), IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient15, DoctorId = SeedIds.Doctor5,
                FirstName = "Наташа", LastName = "Илиевска",
                NationalId = "2107880123466", SzboNumber = "SZBO-000015",
                BirthDate = new DateTime(1988, 7, 21), Gender = Gender.Female,
                City = "Kavadarci", Address = "ул. Македонија 31", PostalCode = "1430",
                Phone = "+38970666015", Email = "natasa.ilievska@email.com",
                EmergencyContactName = "Драган Илиевски", EmergencyContactPhone = "+38970666025",
                EmergencyRelationship = "Сопруг", BloodType = "AB-", Allergies = "Нема",
                Status = PatientStatus.Active, RegistrationDate = now.AddDays(-13), IsDeleted = false
            },
        };

        // Keep the demo patient catalogue limited to fifteen seeded patients.
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