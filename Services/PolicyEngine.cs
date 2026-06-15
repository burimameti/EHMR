using EHMR.Constants;
using EHMR.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    public class PolicyEngine : IPolicyEngine
    {
        public bool HasPermission(IEnumerable<string> userPermissions, string permission)
        {
            if(userPermissions==null)
                return false;

            return userPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        public bool CanAccessModule(IEnumerable<string> userPermissions, string module)
        {
            if(userPermissions==null)
                return false;

            return PolicyRegistry.Policies.Any(p =>
                p.Module.Equals(module, StringComparison.OrdinalIgnoreCase)&&
                userPermissions.Contains(p.Permission, StringComparer.OrdinalIgnoreCase));
        }

        public IReadOnlyList<string> ResolveModules(IEnumerable<string> userPermissions)
        {
            if(userPermissions==null)
                return new List<string>();

            return PolicyRegistry.Policies
                .Where(p => userPermissions.Contains(p.Permission, StringComparer.OrdinalIgnoreCase))
                .Select(p => p.Module)
                .Distinct()
                .ToList();
        }
    }
}