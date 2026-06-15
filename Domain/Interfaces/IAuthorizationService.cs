using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Interfaces
{
    public interface IAuthorizationService
    {
        bool IsAuthenticated
        {
            get;
        }

        bool RoleHasPermission(UserRole role, string permission);

        bool HasPermission(User user, string permission);

        bool ApplyPositionRules(User user, string permission, bool current);

        IEnumerable<string> Get(UserRole role);

        bool HasRole(string role);

        bool HasAnyRole(params string[] roles);

        bool HasScope(string scope);

        bool HasAnyScope(params string[] scopes);

        bool HasAllScopes(params string[] scopes);

        bool CanAccess(string module);
    }
}