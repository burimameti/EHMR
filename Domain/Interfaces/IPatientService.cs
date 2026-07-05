using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Interfaces
{
    public interface IPatientService
    {
        Task UpdatePatientAsync(Patient cycle);

        Task<List<Patient>> GetAllAsync();

        Task DeleteAsync(Guid id);
    }
}