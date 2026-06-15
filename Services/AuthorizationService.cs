using System;
using System.Collections.Generic;
using System.Linq;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;

namespace EHMR.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IAuthStateService _auth;

    public AuthorizationService(IAuthStateService auth)
    {
        _auth=auth;
    }

    public bool IsAuthenticated => _auth.IsAuthenticated;

    public bool HasRole(string role)
    {
        return _auth.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasAnyRole(params string[] roles)
    {
        return roles.Any(r => _auth.Roles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }

    public bool HasScope(string scope)
    {
        return _auth.Permissions.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasAnyScope(params string[] scopes)
    {
        return scopes.Any(scope => _auth.Permissions.Contains(scope, StringComparer.OrdinalIgnoreCase));
    }

    public bool HasAllScopes(params string[] scopes)
    {
        return scopes.All(scope => _auth.Permissions.Contains(scope, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Основно јадро за детерминирање дозволи за специфичен корисник врз база на 8-те модули.
    /// </summary>
    public bool HasPermission(User user, string moduleKey)
    {
        if(user==null||!user.IsActive)
            return false;

        // 1. Изврши ја паметната хибридна проверка од доменот (експлицитни правила или дефолтни атрибути)
        bool allowed = user.IsAuthorizedToModule(moduleKey);

        // 2. Аплицирање на медицински хиерархиски правила (Clinical & Position Override)
        allowed=ApplyPositionRules(user, moduleKey, allowed);

        return allowed;
    }

    public bool RoleHasPermission(UserRole role, string moduleKey)
    {
        return Modules.GetDefaultsForRole(role).Contains(moduleKey, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> Get(UserRole role)
    {
        return Modules.GetDefaultsForRole(role);
    }

    /// <summary>
    /// Клинички бизнис правила базирани на позиции (UserPosition).
    /// </summary>
    public bool ApplyPositionRules(User user, string moduleKey, bool current)
    {
        // Ако корисникот е SuperAdmin, тој има безусловен пристап до апсолутно сè
        if(user.Position==UserPosition.SuperAdmin)
        {
            return true;
        }

        if(user.Position==UserPosition.Primarius)
        {
            // На пример: Примариус има авторитет секогаш да пристапи до менаџмент на терапии,
            // извештаи и протоколи, дури и ако некој му ги исклучил експлицитно во базата
            if(string.Equals(moduleKey, Modules.Therapy, StringComparison.OrdinalIgnoreCase)||
                string.Equals(moduleKey, Modules.Protocols, StringComparison.OrdinalIgnoreCase)||
                string.Equals(moduleKey, Modules.Reports, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return current;
    }

    public bool CanAccess(string moduleKey)
    {
        // Проверка базирана на моменталната сесија на најавениот корисник
        return _auth.Permissions.Contains(moduleKey, StringComparer.OrdinalIgnoreCase);
    }
}