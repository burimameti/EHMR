using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

public class UserSeeder : IEntitySeeder
{
    /// <summary>
    /// Стандардна лозинка за сите освен администраторот. Докторите и сестрата
    /// досега воопшто немаа поставена лозинка — полето остануваше празно и
    /// најавата беше невозможна.
    ///
    /// Се чува во отворен текст, како и досега: LoginViewModel споредува директно.
    /// </summary>
    private const string DefaultPassword = "123456";

    public int Order => 1;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Users.AnyAsync(ct))
            return;

        var users = new List<User>
        {
            new()
            {
                Id = SeedIds.AdminUser,
                Username = "admin",
                PasswordHash = "123456",
                FirstName = "Игор",
                LastName = "Ангеловски",
                Role = UserRole.Admin,
                Position = UserPosition.SuperAdmin,
                IsActive = true
            },

            new() { Id = SeedIds.DocUser1, Username="dr.mitrev", FirstName="Никола", LastName="Митрев", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash="123456" },
            new() { Id = SeedIds.DocUser2, Username="dr.anastoj", FirstName="Ана", LastName="Стојанова", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true , PasswordHash="123456" },
            new() { Id = SeedIds.DocUser3, Username="dr.goran", FirstName="Горан", LastName="Петров", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true , PasswordHash = "123456"},
            new() { Id = SeedIds.DocUser4, Username="dr.elena", FirstName="Елена", LastName="Костова", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true , PasswordHash = "123456"},
            new() { Id = SeedIds.DocUser5, Username="dr.ivan", FirstName="Иван", LastName="Димитров", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true , PasswordHash = "123456"},
            new() { Id = SeedIds.DocUser6, Username="dr.marija", FirstName="Марија", LastName="Трајкова", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true , PasswordHash = "123456"},
            new() { Id = SeedIds.DocUser7, Username="dr.dejan", FirstName="Дејан", LastName="Стојков", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true  , PasswordHash="123456"   },
            new() { Id = SeedIds.DocUser8, Username="dr.sara", FirstName="Сара", LastName="Јованова", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true , PasswordHash = "123456"},
            new() { Id = SeedIds.DocUser9, Username="dr.vlatko", FirstName="Влатко", LastName="Николов", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true , PasswordHash = "123456"},
            new() { Id = SeedIds.DocUser10, Username="dr.jovana", FirstName="Јована", LastName="Ристовска", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true , PasswordHash = "123456"},

            new()
            {
                Id = SeedIds.NurseUser,
                Username = "nurse.main",
                FirstName = "Сестра",
                LastName = "Главна",
                Role = UserRole.MainNurse,
                Position = UserPosition.Regular,
                IsActive = true,  PasswordHash="123456"
            }
        };

        // Админот си ја задржува својата лозинка; сите останати ја добиваат
        // стандардната.
        foreach(var user in users.Where(u => u.Id!=SeedIds.AdminUser))
            user.PasswordHash=DefaultPassword;

        await context.Users.AddRangeAsync(users, ct);
        await context.SaveChangesAsync(ct);
    }
}