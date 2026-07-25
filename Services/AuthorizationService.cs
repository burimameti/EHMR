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
   

    public bool IsScopedToOwnData =>
        IsAuthenticated
        &&!HasRole(UserRole.Admin)
        &&!HasRole(UserRole.SuperAdmin);
    public bool CanPerform(string module, ModuleAction action)
    {
        if(!IsAuthenticated||_auth.CurrentUser==null)
            return false;

        // Мора прво воопшто да има пристап до модулот (explicit Modules override или role default)
        if(!CanAccessModule(module))
            return false;

        if(HasRole(UserRole.SuperAdmin))
            return true;

        var allowed = RolePermissionMatrix.GetActions(module, _auth.CurrentUser.Role);
        return allowed.HasFlag(action);
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
        if(!IsAuthenticated)
            return false;

        // Admin bypass
        if(HasRole(UserRole.Admin)||
            HasRole(UserRole.SuperAdmin))
        {
            return true;
        }

        return HasModule(module);
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

        return CanAccessModule(module);
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