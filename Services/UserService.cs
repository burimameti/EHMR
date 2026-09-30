using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Services;

public class UserService : IUserService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

    public UserService(IDbContextFactory<DesktopTherapyDbContext> factory)
    {
        _factory=factory;
    }

    public async Task<List<UserAdminDto>> GetUsersAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Users
            .Include(x => x.Modules)
            .Select(x => new UserAdminDto
            {
                Id=x.Id,
                Username=x.Username,
                FirstName=x.FirstName,
                LastName=x.LastName,
                Role=x.Role,
                Position=x.Position,
                IsActive=x.IsActive,
                Modules=x.Modules
                    .Where(m => m.IsEnabled)
                    .Select(m => m.ModuleKey)
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<UserAdminDto?> GetByIdAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Users
            .Include(x => x.Modules)
            .Where(x => x.Id==id)
            .Select(x => new UserAdminDto
            {
                Id=x.Id,
                Username=x.Username,
                FirstName=x.FirstName,
                LastName=x.LastName,
                Role=x.Role,
                Position=x.Position,
                IsActive=x.IsActive,
                Modules=x.Modules
                    .Where(m => m.IsEnabled)
                    .Select(m => m.ModuleKey)
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(UserAdminDto dto)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var user = new User
        {
            Id=Guid.NewGuid(),
            Username=dto.Username,
            FirstName=dto.FirstName,
            LastName=dto.LastName,
            Role=dto.Role,
            Position=dto.Position,
            IsActive=dto.IsActive
        };

        db.Users.Add(user);

        foreach(var module in dto.Modules.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            db.UserModules.Add(new Module
            {
                UserId=user.Id,
                ModuleKey=module,
                IsEnabled=true
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserAdminDto dto)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var user = await db.Users
            .Include(x => x.Modules)
            .FirstAsync(x => x.Id==dto.Id);

        user.Username=dto.Username;
        user.FirstName=dto.FirstName;
        user.LastName=dto.LastName;
        user.Role=dto.Role;
        user.Position=dto.Position;
        user.IsActive=dto.IsActive;

        db.UserModules.RemoveRange(user.Modules);

        foreach(var module in dto.Modules.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            db.UserModules.Add(new Module
            {
                UserId=user.Id,
                ModuleKey=module,
                IsEnabled=true
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var user = await db.Users
            .FirstAsync(x => x.Id==id);

        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }

    public Task<List<string>> GetAllModulesAsync()
    {
        return Task.FromResult(new List<string>
        {
            Modules.Dashboard,
            Modules.Patients,
            Modules.Therapy,
            Modules.Inventory,
            Modules.Administration
        });
    }
}