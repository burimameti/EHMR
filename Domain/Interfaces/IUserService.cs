using EHMR.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Domain.Interfaces
{
    public interface IUserService
    {
        Task<List<UserAdminDto>> GetUsersAsync();

        Task<UserAdminDto?> GetByIdAsync(Guid id);

        Task CreateAsync(UserAdminDto user);

        Task UpdateAsync(UserAdminDto user);
        Task DeleteAsync(Guid d);
        Task<List<string>> GetAllModulesAsync();
    }
}