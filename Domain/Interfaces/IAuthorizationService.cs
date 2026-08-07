using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Domain.Interfaces;

public interface IAuthorizationService
{
    bool IsAuthenticated
    {
        get;
    }
  
    bool IsScopedToOwnData
    {
        get;
    }
    /// <summary>
    /// The Doctor.Id linked to the current user, or null if the current user
    /// is not a Doctor (or has no linked Doctor record).
    /// </summary>
    Guid? CurrentDoctorId
    {
        get;
    }
    bool CanPerform(string module, ModuleAction action);
    bool HasRole(UserRole role);
    bool HasModule(string module);
    bool CanAccessModule(string module);
    bool CanAccessRoute(string route);

    // --- RBAC: управување со корисници ---
    bool CanManageUser(UserRole targetRole);
    bool CanAssignRole(UserRole targetRole);
    IEnumerable<UserRole> GetAssignableRoles();
}