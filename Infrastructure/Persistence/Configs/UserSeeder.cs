using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

public class UserSeeder : IEntitySeeder
{
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
                Id=SeedIds.AdminUser,
                Username="admin",
                PasswordHash=DefaultPassword,
                FirstName="Admin",
                LastName="Admin",
                Role=UserRole.Admin,
                Position=UserPosition.Regular,
                IsActive=true
            },
            new()
            {
                Id=SeedIds.SuperAdminUser,
                Username="SuperAdmin",
                PasswordHash=DefaultPassword,
                FirstName="SuperAdmin",
                LastName="SuperAdmin",
                Role=UserRole.SuperAdmin,
                Position=UserPosition.Regular,
                IsActive=true
            },
            new() { Id=SeedIds.DocUser1, Username="dr.mitrev", FirstName="Никола", LastName="Митрев", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash=DefaultPassword },
            new() { Id=SeedIds.DocUser2, Username="dr.anastoj", FirstName="Ана", LastName="Стојанова", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash=DefaultPassword },
            new() { Id=SeedIds.DocUser3, Username="dr.goran", FirstName="Горан", LastName="Петров", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash=DefaultPassword },
            new() { Id=SeedIds.DocUser4, Username="dr.elena", FirstName="Елена", LastName="Костова", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash=DefaultPassword },
            new() { Id=SeedIds.DocUser5, Username="dr.ivan", FirstName="Иван", LastName="Димитров", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash=DefaultPassword },
            new() { Id=SeedIds.DocUser6, Username="dr.marija", FirstName="Марија", LastName="Трајкова", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash=DefaultPassword },
            new() { Id=SeedIds.DocUser7, Username="dr.dejan", FirstName="Дејан", LastName="Стојков", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash=DefaultPassword },
            new() { Id=SeedIds.DocUser8, Username="dr.sara", FirstName="Сара", LastName="Јованова", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash=DefaultPassword },
            new() { Id=SeedIds.DocUser9, Username="dr.vlatko", FirstName="Влатко", LastName="Николов", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash=DefaultPassword },
            new() { Id=SeedIds.DocUser10, Username="dr.jovana", FirstName="Јована", LastName="Ристовска", Role=UserRole.Doctor, Position=UserPosition.Regular, IsActive=true, PasswordHash=DefaultPassword },
            new()
            {
                Id=SeedIds.NurseUser,
                Username="nurse.main",
                FirstName="Сестра",
                LastName="Главна",
                Role=UserRole.MainNurse,
                Position=UserPosition.Regular,
                IsActive=true,
                PasswordHash=DefaultPassword
            }
        };

        await context.Users.AddRangeAsync(users, ct);
        await context.SaveChangesAsync(ct);
    }
}