using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    public class UserService : IUserService
    {
        private readonly DesktopTherapyDbContext _context;

        public UserService(DesktopTherapyDbContext context)
        {
            _context=context;
        }

        public async Task<List<UserAdminDto>> GetUsersAsync()
        {
            return await _context.Users
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
            return await _context.Users
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

            _context.Users.Add(user);

            foreach(var module in dto.Modules)
            {
                _context.UserModules.Add(new Module
                {
                    UserId=user.Id,
                    ModuleKey=module,
                    IsEnabled=true
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserAdminDto dto)
        {
            var user = await _context.Users
                .Include(x => x.Modules)
                .FirstAsync(x => x.Id==dto.Id);

            user.Username=dto.Username;
            user.FirstName=dto.FirstName;
            user.LastName=dto.LastName;
            user.Role=dto.Role;
            user.Position=dto.Position;
            user.IsActive=dto.IsActive;

            // sync modules
            _context.UserModules.RemoveRange(user.Modules);

            foreach(var module in dto.Modules)
            {
                _context.UserModules.Add(new Module
                {
                    UserId=user.Id,
                    ModuleKey=module,
                    IsEnabled=true
                });
            }

            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Guid Id)
        {
            var user = await _context.Users
                .Include(x => x.Role)
                .FirstAsync(x => x.Id==Id);

           

            // sync modules
            await _context.Users.ExecuteDeleteAsync();         

            await _context.SaveChangesAsync();
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
}