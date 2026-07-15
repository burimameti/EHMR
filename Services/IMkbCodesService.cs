using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{

    public interface IMkb10CodeService
    {
        Task<List<Mkb10Code>> GetAllAsync();
        Task<Mkb10Code?> GetByIdAsync(Guid id);
        Task SaveAsync(Mkb10Code code);
        Task DeleteAsync(Guid id);
        Task<List<Mkb10Code>> SearchAsync(string query, CancellationToken ct = default);
    }
}
