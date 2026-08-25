using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
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
            .Where(u =>
                u.Role==UserRole.Doctor&&
                u.IsActive)
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Take(15)
            .ToListAsync(ct);

        if(users.Count<10)
            return;

        var doctors = new[]
   {
    CreateDoctor(
        SeedIds.Doctor1,
        users[0],
        "+38970111111",
        "doctor1@ehmr.mk"),

    CreateDoctor(
        SeedIds.Doctor2,
        users[1],
        "+38970111112",
        "doctor2@ehmr.mk"),

    CreateDoctor(
        SeedIds.Doctor3,
        users[2],
        "+38970111113",
        "doctor3@ehmr.mk"),

    CreateDoctor(
        SeedIds.Doctor4,
        users[3],
        "+38970111114",
        "doctor4@ehmr.mk"),

    CreateDoctor(
        SeedIds.Doctor5,
        users[4],
        "+38970111115",
        "doctor5@ehmr.mk"),

    CreateDoctor(
        SeedIds.Doctor6,
        users[5],
        "+38970111116",
        "doctor6@ehmr.mk"),

    CreateDoctor(
        SeedIds.Doctor7,
        users[6],
        "+38970111117",
        "doctor7@ehmr.mk"),

    CreateDoctor(
        SeedIds.Doctor8,
        users[7],
        "+38970111118",
        "doctor8@ehmr.mk"),

    CreateDoctor(
        SeedIds.Doctor9,
        users[8],
        "+38970111119",
        "doctor9@ehmr.mk"),

    CreateDoctor(
        SeedIds.Doctor10,
        users[9],
        "+38970111120",
        "doctor10@ehmr.mk")
};

        await context.Doctors.AddRangeAsync(
            doctors,
            ct);

        await context.SaveChangesAsync(ct);
    }
    private static Doctor CreateDoctor(
        Guid id,
        User user,
        string phone,
        string email)
    {
        return new Doctor
        {
            Id=id,

            UserId=user.Id,
            User=user,

            ContactPhone=phone,
            Email=email,

            Gender=Gender.Male,
            Status=Status.Active
        };
    }
}