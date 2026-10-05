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

        var users = await db.Users
            .Include(x => x.Modules)
            .Include(x => x.ModulePermissions)
            .ToListAsync();

        return users.Select(MapDto).ToList();
    }

    public async Task<UserAdminDto?> GetByIdAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var user = await db.Users
            .Include(x => x.Modules)
            .Include(x => x.ModulePermissions)
            .FirstOrDefaultAsync(x => x.Id==id);

        return user is null ? null : MapDto(user);
    }

    private static UserAdminDto MapDto(User user)
    {
        return new UserAdminDto
        {
            Id=user.Id,
            Username=user.Username,
            FirstName=user.FirstName,
            LastName=user.LastName,
            Role=user.Role,
            Position=user.Position,
            IsActive=user.IsActive,
            Modules=user.Modules
                .Where(m => m.IsEnabled)
                .Select(m => m.ModuleKey)
                .ToList(),
            Permissions=user.ModulePermissions.ToDictionary(
                p => p.ModuleKey,
                p => p.Actions,
                StringComparer.OrdinalIgnoreCase)
        };
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

        if(dto.Permissions is not null)
        {
            foreach(var permission in dto.Permissions)
            {
                db.UserModulePermissions.Add(new UserModulePermission
                {
                    UserId=user.Id,
                    ModuleKey=permission.Key,
                    Actions=permission.Value
                });
            }
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

        // Keep the new permission assignments optional until the administration UI
        // starts supplying them. Existing user edits must not wipe permissions.
        if(dto.Permissions is not null)
        {
            db.UserModulePermissions.RemoveRange(user.ModulePermissions);

            foreach(var permission in dto.Permissions)
            {
                db.UserModulePermissions.Add(new UserModulePermission
                {
                    UserId=user.Id,
                    ModuleKey=permission.Key,
                    Actions=permission.Value
                });
            }
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
            Modules.Medicines,
            Modules.Administration
        });
    }
}