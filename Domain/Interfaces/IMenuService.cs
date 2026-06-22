using EHMR.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Interfaces
{
    public interface IMenuService
    {
        Task<List<NavigationGroup>> UpdateMenuAsync(
          );
    }
}