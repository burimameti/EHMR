using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;

namespace EHMR.Domain.Interfaces;

public interface IAuthorizationService
{
    bool IsAuthenticated
    {
        get;
    }

    bool HasRole(UserRole role);

    bool HasModule(string module);

    bool CanAccessModule(string module);

    bool CanAccessRoute(string route);
}