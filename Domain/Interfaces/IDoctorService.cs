using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Interfaces
{
    public interface IDoctorService
    {
        Task<List<Doctor>> GetAllAsync();

        Task<Doctor?> GetByIdAsync(Guid id);

        Task AddAsync(Doctor doctor);

        Task UpdateAsync(Doctor doctor);

        Task DeleteAsync(Guid id);
    }
}
