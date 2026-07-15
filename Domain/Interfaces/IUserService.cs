using EHMR.Services;

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