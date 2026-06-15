using System;
using System.Linq;
using EHMR.Constants;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Extensions;

namespace EHMR.Services;

public interface IAuthorizationPolicy
{
    bool CanAccess(string route);
}

public class AuthorizationPolicy : IAuthorizationPolicy
{
    private readonly IAuthStateService _auth;

    public AuthorizationPolicy(IAuthStateService auth)
    {
        _auth=auth;
    }

    public bool CanAccess(string route)
    {
        // 1. Ако не е автентициран, дозволи само најава
        if(!_auth.IsAuthenticated)
        {
            return string.Equals(route, "login", StringComparison.OrdinalIgnoreCase);
        }

        // Чистење на рутата (ако имаш празни места или специфичен формат од Shell)
        if(string.IsNullOrWhiteSpace(route)) return false;
        var cleanRoute = route.Trim().ToLowerScalarOrOrdinal(); // или прост .Trim()

        // 2. Брз пристап до дефолтниот Dashboard
        if(cleanRoute=="dashboard"||cleanRoute==AppRoutes.Dashboard.ToLower())
        {
            return _auth.Permissions.Contains(Modules.Dashboard, StringComparer.OrdinalIgnoreCase);
        }

        // 3. НАПРЕДЕН ДИНАМИЧЕН ЛУКАП:
        // Бараме низ сите навигациони групи и нивните под-елементи во AppNavigation
        // за да најдеме на кој ДОМЕНСКИ МОДУЛ му припаѓа оваа рута.
        var targetModule = AppNavigation.AllGroups
            .SelectMany(group => group.Items.Select(item => new { item.Route, item.Module })
                .Append(new
                {
                    Route = group.Route,
                    Module = group.Module
                })) // Ја земаме и рутата на самата група (ако има)
            .Where(x => !string.IsNullOrEmpty(x.Route))
            .FirstOrDefault(x => cleanRoute.Contains(x.Route.ToLower(), StringComparison.OrdinalIgnoreCase))?.Module;

        // 4. Ако рутата не е заштитена со конкретен модул во навигацијата, дозволи пристап
        if(string.IsNullOrEmpty(targetModule))
        {
            return true;
        }

        // 5. Провери дали најавениот корисник го има тој модул во својата сесија
        return _auth.Permissions.Contains(targetModule, StringComparer.OrdinalIgnoreCase);
    }

    // Помошна екстензија за полесно чистење на стрингови (опционално)
}