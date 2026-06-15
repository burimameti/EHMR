using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Seeders;

public class PatientSeeder : IEntitySeeder
{
    int IEntitySeeder.Order => 4;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Patients.AnyAsync(ct))
            return;

        var patients = new List<Patient>
        {
            new Patient
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FirstName = "Marko",
                LastName = "Stojanov",
                NationalId = "MK1234567",
                SSN = "0101995123456",
                BirthDate = new DateTime(1995, 1, 1),
                Gender = Gender.Male,

                Phone = "+38970111222",
                Email = "marko.stojanov@email.com",
                Address = "Ul. Partizanska 12",
                City = "Skopje",
                PostalCode = "1000",

                EmergencyContactName = "Ana Stojanova",
                EmergencyContactPhone = "+38970111999",
                EmergencyRelationship = "Sister",

                BloodType = "A+",
                Allergies = "None",
                PrimaryDiagnosis = "Lumbar disc herniation",
                ClinicalNotes = "Initial physiotherapy recommended",

                Status = PatientStatus.Active,
                RegistrationDate = DateTime.UtcNow,
                IsDeleted = false
            },

            new Patient
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FirstName = "Elena",
                LastName = "Kostova",
                NationalId = "MK7654321",
                SSN = "0202988123456",
                BirthDate = new DateTime(1988, 2, 2),
                Gender = Gender.Female,

                Phone = "+38971222333",
                Email = "elena.kostova@email.com",
                Address = "Ul. Goce Delcev 5",
                City = "Bitola",
                PostalCode = "7000",

                EmergencyContactName = "Petar Kostov",
                EmergencyContactPhone = "+38971222999",
                EmergencyRelationship = "Husband",

                BloodType = "B-",
                Allergies = "Penicillin",
                PrimaryDiagnosis = "Knee osteoarthritis",
                ClinicalNotes = "Chronic condition, rehab program ongoing",

                Status = PatientStatus.Active,
                RegistrationDate = DateTime.UtcNow,
                IsDeleted = false
            },

            new Patient
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                FirstName = "Nikola",
                LastName = "Dimitrov",
                NationalId = "MK9988776",
                SSN = "1505990123456",
                BirthDate = new DateTime(1990, 5, 15),
                Gender = Gender.Male,

                Phone = "+38973333444",
                Email = "nikola.dimitrov@email.com",
                Address = "Ul. Ilindenska 20",
                City = "Strumica",
                PostalCode = "2400",

                EmergencyContactName = "Maja Dimitrova",
                EmergencyContactPhone = "+38973333999",
                EmergencyRelationship = "Wife",

                BloodType = "O+",
                Allergies = "Dust allergy",
                PrimaryDiagnosis = "Cervical pain syndrome",
                ClinicalNotes = "Postural correction therapy required",

                Status = PatientStatus.Active,
                RegistrationDate = DateTime.UtcNow,
                IsDeleted = false
            }
        };

        await context.Patients.AddRangeAsync(patients, ct);

        // IMPORTANT: save once per seeder (recommended)
        await context.SaveChangesAsync(ct);
    }
}