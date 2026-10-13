using EHMR.Constants;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;

namespace EHMR.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IAuthStateService _auth;

    public AuthorizationService(IAuthStateService auth)
    {
        _auth=auth;
    }
   

    // Only doctors are scoped to their own clinical records. Main nurses and
    // nurses need clinic-wide read access to patients, appointments and encounters.
    public bool IsScopedToOwnData =>
        IsAuthenticated
        && HasRole(UserRole.Doctor);
    public bool CanPerform(string module, ModuleAction action)
    {
        if(!IsAuthenticated || _auth.CurrentUser is null || action == ModuleAction.None)
            return false;

        // Non-administrator clinical roles must never delete records anywhere.
        if(action == ModuleAction.Delete
            && !HasRole(UserRole.Admin)
            && !HasRole(UserRole.SuperAdmin))
            return false;

        // Doctors and nursing roles may review appointments/encounters but may
        // not correct existing records, even if stale explicit grants exist.
        if(action == ModuleAction.Edit
            && (HasRole(UserRole.Doctor) || HasRole(UserRole.MainNurse) || HasRole(UserRole.Nurse))
            && (string.Equals(module, Modules.Appointments, StringComparison.OrdinalIgnoreCase)
                || string.Equals(module, Modules.Encounters, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Medicines are a shared clinic catalog. Creating medicines is intentionally
        // available to every authenticated user; it is not an RBAC privilege.
        if(string.Equals(module, Modules.Medicines, StringComparison.OrdinalIgnoreCase)
            && action == ModuleAction.Create)
            return true;

        if(HasRole(UserRole.SuperAdmin)||HasRole(UserRole.Admin))
            return true;

        // Built-in role permissions must remain effective even when the user
        // has persisted module-permission rows containing None (legacy/default
        // rows). Explicit permissions may add actions, but must not erase the
        // role's configured baseline privileges.
        var roleDefaults = RolePermissionMatrix.GetActions(module, _auth.CurrentUser.Role);
        var explicitPermission = GetExplicitPermission(module);
        var allowed = roleDefaults | (explicitPermission ?? ModuleAction.None);

        if(allowed.HasFlag(action))
            return true;

        // Do not allow persisted user-module assignments to grant actions that
        // are not part of the role baseline unless they are explicitly present.
        if(explicitPermission.HasValue && explicitPermission.Value.HasFlag(action))
            return true;

        return false;
    }
    public bool IsAuthenticated
        => _auth.IsAuthenticated;

    public Guid? CurrentDoctorId => _auth.CurrentDoctorId;

    // ===================================
    // ROLE
    // ===================================
    public bool HasRole(UserRole role)
    {
        return _auth.CurrentUser?.Role==role;
    }

    // ===================================
    // MODULE
    // ===================================
    public bool HasModule(string module)
    {
        return _auth.CurrentUser?.IsAuthorizedToModule(module)==true;
    }

    public bool CanAccessModule(string module)
    {
        if(!IsAuthenticated || _auth.CurrentUser is null)
            return false;

        if(HasRole(UserRole.SuperAdmin))
            return true;

        if(HasRole(UserRole.Admin))
            return true;

        // A role's configured baseline is authoritative for its default
        // modules; legacy persisted rows with Actions=None must not hide them.
        if(RolePermissionMatrix.GetActions(module, _auth.CurrentUser.Role) != ModuleAction.None)
            return true;

        var explicitPermission = GetExplicitPermission(module);
        if(explicitPermission.HasValue)
            return explicitPermission.Value != ModuleAction.None;

        return HasModule(module);
    }

    private ModuleAction? GetExplicitPermission(string module)
    {
        var permissions = _auth.CurrentUser?.ModulePermissions;
        if(permissions is null)
            return null;

        var permission = permissions.FirstOrDefault(x =>
            string.Equals(x.ModuleKey, module, StringComparison.OrdinalIgnoreCase));

        return permission?.Actions;
    }

    // ===================================
    // ROUTE SECURITY
    // ===================================
    public bool CanAccessRoute(string route)
    {
        if(string.IsNullOrWhiteSpace(route))
            return false;

        route=Normalize(route);

        // Guest routes
        if(!IsAuthenticated)
        {
            return route=="login";
        }

        var module = ResolveModule(route);

        // Route without module mapping
        if(module is null)
            return true;

        return CanPerform(module, ModuleAction.View);
    }

    // ===================================
    // RBAC: УПРАВУВАЊЕ СО КОРИСНИЦИ
    // ===================================

    /// <summary>
    /// Дали тековно најавениот корисник смее да управува (edit/delete) со корисник кој ја има улогата targetRole.
    /// </summary>
    public bool CanManageUser(UserRole targetRole)
    {
        if(!IsAuthenticated||_auth.CurrentUser==null)
            return false;

        return RoleHierarchy.CanManage(_auth.CurrentUser.Role, targetRole);
    }

    /// <summary>
    /// Дали тековно најавениот корисник смее да ја додели улогата targetRole на некого.
    /// </summary>
    public bool CanAssignRole(UserRole targetRole)
    {
        if(!IsAuthenticated||_auth.CurrentUser==null)
            return false;

        return RoleHierarchy.AssignableRolesFor(_auth.CurrentUser.Role).Contains(targetRole);
    }

    /// <summary>
    /// Сите улоги што тековно најавениот корисник смее да ги додели.
    /// </summary>
    public IEnumerable<UserRole> GetAssignableRoles()
    {
        if(!IsAuthenticated||_auth.CurrentUser==null)
            return Enumerable.Empty<UserRole>();

        return RoleHierarchy.AssignableRolesFor(_auth.CurrentUser.Role);
    }

    // ===================================
    // ROUTE → MODULE
    // ===================================
    private string? ResolveModule(string route)
    {
        route=Normalize(route);
        route=Normalize(AppNavigation.ResolveMenuRoute(route));

        var routes = AppNavigation.AllGroups
            .SelectMany(g =>
                g.Items.Select(i => new
                {
                    Route = Normalize(i.Route),
                    Module = i.Module
                })
                .Append(new
                {
                    Route = Normalize(g.Route),
                    Module = g.Module
                }));

        return routes
            .FirstOrDefault(x => x.Route==route)?
            .Module;
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
