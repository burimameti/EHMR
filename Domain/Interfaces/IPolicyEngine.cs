using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Interfaces
{
    public interface IPolicyEngine
    {
        bool HasPermission(IEnumerable<string> userPermissions, string permission);

        bool CanAccessModule(IEnumerable<string> userPermissions, string module);

        IReadOnlyList<string> ResolveModules(IEnumerable<string> userPermissions);
    }
}