using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Domain.Entities.Rbac.AppRoutes;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Seeders;

public class PatientSeeder : IEntitySeeder
{
    int IEntitySeeder.Order => 8; // must run after DoctorSeeder and MedicineSeeder (Order 5)

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Patients.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;

        // Cycles across seeded doctors; adjust count/order to match DoctorSeeder
      
   

        // Cycles across seeded medicines (MedicineSeeder, Order 5)
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
                FirstName = "Jeton",
                LastName = "Ismaili",
                NationalId = "0101990123456",
                BirthDate = new DateTime(1990, 1, 15),
                Gender = Gender.Male,
                City = "Skopje",
                Address = "Ul. Ilindenska 12",
                PostalCode = "1000",
                Phone = "+38970111222",
                Email = "jeton.ismaili@email.com",
                EmergencyContactName = "Fatime Ismaili",
                EmergencyContactPhone = "+38970111223",
                EmergencyRelationship = "Spouse",
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
                FirstName = "Elena",
                LastName = "Petrovska",
                NationalId = "1503988654321",
                BirthDate = new DateTime(1988, 3, 15),
                Gender = Gender.Female,
                City = "Skopje",
                Address = "Bul. Partizanski Odredi 45",
                PostalCode = "1000",
                Phone = "+38970222333",
                Email = "elena.petrovska@email.com",
                EmergencyContactName = "Marko Petrovski",
                EmergencyContactPhone = "+38970222334",
                EmergencyRelationship = "Brother",
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
                FirstName = "Arjeta",
                LastName = "Shabani",
                NationalId = "2207985112233",
                BirthDate = new DateTime(1985, 7, 22),
                Gender = Gender.Female,
                City = "Struga",
                Address = "Ul. Kej Makedonija 8",
                PostalCode = "6330",
                Phone = "+38975333444",
                Email = "arjeta.shabani@email.com",
                EmergencyContactName = "Driton Shabani",
                EmergencyContactPhone = "+38975333445",
                EmergencyRelationship = "Husband",
                BloodType = "B-",
                Allergies = "Penicillin",
                Status = PatientStatus.Chronic,
                RegistrationDate = now.AddDays(-28),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient4,
                   DoctorId = SeedIds.Doctor4,
                FirstName = "Muharem",
                LastName = "Rexhepi",
                NationalId = "0512982445566",
                BirthDate = new DateTime(1982, 12, 5),
                Gender = Gender.Male,
                City = "Gostivar",
                Address = "Ul. Mladinska 3",
                PostalCode = "1230",
                Phone = "+38970444555",
                Email = "muharem.rexhepi@email.com",
                EmergencyContactName = "Sara Rexhepi",
                EmergencyContactPhone = "+38970444556",
                EmergencyRelationship = "Жена",
                BloodType = "O+",
                Allergies = "Dust",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-26),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient5,
                    DoctorId = SeedIds.Doctor5,
                FirstName = "Tomislav",
                LastName = "Nikolovski",
                NationalId = "1809991778899",
                BirthDate = new DateTime(1991, 9, 18),
                Gender = Gender.Male,
                City = "Skopje",
                Address = "Ul. 11 Oktomvri 21",
                PostalCode = "1000",
                Phone = "+38970555666",
                Email = "tomislav.nikolovski@email.com",
                EmergencyContactName = "Ana Nikolovska",
                EmergencyContactPhone = "+38970555667",
                EmergencyRelationship = "Sister",
                BloodType = "AB+",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-24),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient6,
            DoctorId = SeedIds.Doctor6,
                FirstName = "Biljana",
                LastName = "Trajkovska",
                NationalId = "2711987991122",
                BirthDate = new DateTime(1987, 11, 27),
                Gender = Gender.Female,
                City = "Kumanovo",
                Address = "Ul. Boris Kidric 15",
                PostalCode = "1300",
                Phone = "+38970666777",
                Email = "biljana.trajkovska@email.com",
                EmergencyContactName = "Vlado Trajkovski",
                EmergencyContactPhone = "+38970666778",
                EmergencyRelationship = "Husband",
                BloodType = "O-",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-22),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient7,
                   DoctorId = SeedIds.Doctor7,
                FirstName = "Milena",
                LastName = "Stojanovska",
                NationalId = "0304984223344",
                BirthDate = new DateTime(1984, 4, 3),
                Gender = Gender.Female,
                City = "Ohrid",
                Address = "Ul. Car Samoil 9",
                PostalCode = "6000",
                Phone = "+38975777888",
                Email = "milena.stojanovska@email.com",
                EmergencyContactName = "Petar Stojanovski",
                EmergencyContactPhone = "+38975777889",
                EmergencyRelationship = "Father",
                BloodType = "A-",
                Allergies = "Latex",
                Status = PatientStatus.Chronic,
                RegistrationDate = now.AddDays(-20),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient8,
                   DoctorId = SeedIds.Doctor8,
                FirstName = "Bekim",
                LastName = "Ahmeti",
                NationalId = "1902983556677",
                BirthDate = new DateTime(1983, 2, 19),
                Gender = Gender.Male,
                City = "Tetovo",
                Address = "Ul. Ilindenska 30",
                PostalCode = "1200",
                Phone = "+38970888999",
                Email = "bekim.ahmeti@email.com",
                EmergencyContactName = "Lindita Ahmeti",
                EmergencyContactPhone = "+38970888990",
                EmergencyRelationship = "Wife",
                BloodType = "B+",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-18),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient9,
                   DoctorId = SeedIds.Doctor9,
                FirstName = "Besa",
                LastName = "Krasniqi",
                NationalId = "2606986889900",
                BirthDate = new DateTime(1986, 6, 26),
                Gender = Gender.Female,
                City = "Veles",
                Address = "Ul. Panko Brashnarov 5",
                PostalCode = "1400",
                Phone = "+38970999000",
                Email = "besa.krasniqi@email.com",
                EmergencyContactName = "Arben Krasniqi",
                EmergencyContactPhone = "+38970999001",
                EmergencyRelationship = "Brother",
                BloodType = "A+",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-16),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient10,
                   DoctorId = SeedIds.Doctor10,
                FirstName = "Imran",
                LastName = "Osmani",
                NationalId = "1408980112200",
                BirthDate = new DateTime(1980, 8, 14),
                Gender = Gender.Male,
                City = "Struga",
                Address = "Ul. Bratstvo Edinstvo 2",
                PostalCode = "6330",
                Phone = "+38975000111",
                Email = "imran.osmani@email.com",
                EmergencyContactName = "Fatbardha Osmani",
                EmergencyContactPhone = "+38975000112",
                EmergencyRelationship = "Wife",
                BloodType = "O+",
                Allergies = "Нема",
                Status = PatientStatus.Inactive,
                RegistrationDate = now.AddDays(-14),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient11,
                 DoctorId = SeedIds.Doctor1,
                FirstName = "Sara",
                LastName = "Angelova",
                NationalId = "0705992334455",
                BirthDate = new DateTime(1992, 5, 7),
                Gender = Gender.Female,
                City = "Skopje",
                Address = "Ul. Vasil Glavinov 18",
                PostalCode = "1000",
                Phone = "+38970111333",
                Email = "sara.angelova@email.com",
                EmergencyContactName = "Igor Angelov",
                EmergencyContactPhone = "+38970111334",
                EmergencyRelationship = "Husband",
                BloodType = "A+",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-12),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient12,
                DoctorId = SeedIds.Doctor4,
                FirstName = "Naim",
                LastName = "Meta",
                NationalId = "1112979445566",
                BirthDate = new DateTime(1979, 12, 11),
                Gender = Gender.Male,
                City = "Bitola",
                Address = "Ul. Kliment Ohridski 40",
                PostalCode = "7000",
                Phone = "+38975222444",
                Email = "naim.meta@email.com",
                EmergencyContactName = "Drita Meta",
                EmergencyContactPhone = "+38975222445",
                EmergencyRelationship = "Wife",
                BloodType = "AB-",
                Allergies = "Shellfish",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-10),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient13,
                DoctorId = SeedIds.Doctor5,
                FirstName = "Ana",
                LastName = "Georgievska",
                NationalId = "2003994556677",
                BirthDate = new DateTime(1994, 3, 20),
                Gender = Gender.Female,
                City = "Prilep",
                Address = "Ul. Marksova 6",
                PostalCode = "7500",
                Phone = "+38975333555",
                Email = "ana.georgievska@email.com",
                EmergencyContactName = "Filip Georgievski",
                EmergencyContactPhone = "+38975333556",
                EmergencyRelationship = "Brother",
                BloodType = "B+",
                Allergies = "Pollen",
                Status = PatientStatus.Chronic,
                RegistrationDate = now.AddDays(-9),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient14,
                DoctorId = SeedIds.Doctor4,
                FirstName = "Goran",
                LastName = "Ilievski",
                NationalId = "2909981667788",
                BirthDate = new DateTime(1981, 9, 29),
                Gender = Gender.Male,
                City = "Skopje",
                Address = "Ul. Franklin Ruzvelt 55",
                PostalCode = "1000",
                Phone = "+38970444666",
                Email = "goran.ilievski@email.com",
                EmergencyContactName = "Snezhana Ilievska",
                EmergencyContactPhone = "+38970444667",
                EmergencyRelationship = "Wife",
                BloodType = "A+",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-8),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient15,
                DoctorId = SeedIds.Doctor4,
                FirstName = "Marija",
                LastName = "Naumovska",
                NationalId = "1601996778899",
                BirthDate = new DateTime(1996, 1, 16),
                Gender = Gender.Female,
                City = "Ohrid",
                Address = "Ul. Grigor Prlicev 11",
                PostalCode = "6000",
                Phone = "+38975555777",
                Email = "marija.naumovska@email.com",
                EmergencyContactName = "Kire Naumovski",
                EmergencyContactPhone = "+38975555778",
                EmergencyRelationship = "Татко",
                BloodType = "O+",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-7),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient16,
                DoctorId = SeedIds.Doctor4,
                FirstName = "Driton",
                LastName = "Berisha",
                NationalId = "0812978889900",
                BirthDate = new DateTime(1978, 12, 8),
                Gender = Gender.Male,
                City = "Tetovo",
                Address = "Ul. Nikola Karev 22",
                PostalCode = "1200",
                Phone = "+38970666888",
                Email = "driton.berisha@email.com",
                EmergencyContactName = "Vjollca Berisha",
                EmergencyContactPhone = "+38970666889",
                EmergencyRelationship = "Wife",
                BloodType = "B-",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-6),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient17,
                DoctorId = SeedIds.Doctor2,
                FirstName = "Ivana",
                LastName = "Dimitrievska",
                NationalId = "0407993990011",
                BirthDate = new DateTime(1993, 7, 4),
                Gender = Gender.Female,
                City = "Kavadarci",
                Address = "Ul. Ljuben Ivanov 14",
                PostalCode = "1430",
                Phone = "+38975666999",
                Email = "ivana.dimitrievska@email.com",
                EmergencyContactName = "Aleksandar Dimitrievski",
                EmergencyContactPhone = "+38975666998",
                EmergencyRelationship = "Husband",
                BloodType = "A-",
                Allergies = "Nuts",
                Status = PatientStatus.Chronic,
                RegistrationDate = now.AddDays(-5),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient18,
                DoctorId = SeedIds.Doctor4,
                FirstName = "Bojan",
                LastName = "Stankovski",
                NationalId = "2202989001122",
                BirthDate = new DateTime(1989, 2, 22),
                Gender = Gender.Male,
                City = "Skopje",
                Address = "Ul. Kuzman Josifovski Pitu 3",
                PostalCode = "1000",
                Phone = "+38970777999",
                Email = "bojan.stankovski@email.com",
                EmergencyContactName = "Elena Stankovska",
                EmergencyContactPhone = "+38970777998",
                EmergencyRelationship = "Wife",
                BloodType = "O+",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-4),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient19,
                DoctorId = SeedIds.Doctor4,
                FirstName = "Elizabeta",
                LastName = "Pavlovska",
                NationalId = "1509975112233",
                BirthDate = new DateTime(1975, 9, 15),
                Gender = Gender.Female,
                City = "Bitola",
                Address = "Ul. Solunska 17",
                PostalCode = "7000",
                Phone = "+38975888111",
                Email = "elizabeta.pavlovska@email.com",
                EmergencyContactName = "Zoran Pavlovski",
                EmergencyContactPhone = "+38975888112",
                EmergencyRelationship = "Husband",
                BloodType = "AB+",
                Allergies = "Нема",
                Status = PatientStatus.Inactive,
                RegistrationDate = now.AddDays(-3),
                IsDeleted = false
            },
            new()
            {
                Id = SeedIds.Patient20,
                DoctorId = SeedIds.Doctor4,
                FirstName = "Dejan",
                LastName = "Lazarevski",
                NationalId = "3011995223344",
                BirthDate = new DateTime(1995, 11, 30),
                Gender = Gender.Male,
                City = "Skopje",
                Address = "Ul. Mito Hadzivasilev Jasmin 9",
                PostalCode = "1000",
                Phone = "+38970999222",
                Email = "dejan.lazarevski@email.com",
                EmergencyContactName = "Marija Lazarevska",
                EmergencyContactPhone = "+38970999223",
                EmergencyRelationship = "Sister",
                BloodType = "A+",
                Allergies = "Нема",
                Status = PatientStatus.Active,
                RegistrationDate = now.AddDays(-2),
                IsDeleted = false
            }
        };

        await context.Patients.AddRangeAsync(patients, ct);

        // One PatientMedicine per patient, recreating the old per-patient DosesFrequency
        // values but now attached to a specific (seeded) medicine instead of floating
        // on the Patient row. Index into `patients` matches the frequency each patient
        // used to carry, in the same order as the original list.
        var frequenciesInOriginalOrder = new[]
        {
            DosesFrequency.Daily, DosesFrequency.TwiceDaily, DosesFrequency.EveryOtherDay, DosesFrequency.Weekly,
            DosesFrequency.Daily, DosesFrequency.ThreeTimesDaily, DosesFrequency.Monthly, DosesFrequency.Daily,
            DosesFrequency.TwiceDaily, DosesFrequency.EveryThreeDays, DosesFrequency.Daily, DosesFrequency.Weekly,
            DosesFrequency.EveryOtherDay, DosesFrequency.Daily, DosesFrequency.TwiceDaily, DosesFrequency.Daily,
            DosesFrequency.Monthly, DosesFrequency.Daily, DosesFrequency.Weekly, DosesFrequency.Daily
        };

        
        

        await context.SaveChangesAsync(ct);
    }
}