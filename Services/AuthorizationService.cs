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

    public bool IsAuthenticated
        => _auth.IsAuthenticated;

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