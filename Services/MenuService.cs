using EHMR.Constants;
using EHMR.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.Services;

public class MenuService : IMenuService
{
    public Task<List<NavigationGroup>> UpdateMenuAsync(
        IEnumerable<string> permissions,
        IEnumerable<string> roles)
    {
        var permissionSet = permissions?.ToHashSet()??new HashSet<string>();
        var roleSet = roles?.ToHashSet()??new HashSet<string>();

        var filteredGroups = AppNavigation.AllGroups
            .Select(group =>
            {
                // 1. Филтрирај ги подменијата (ако ги има)
                var allowedItems = group.Items?
                    .Where(item => IsItemAllowed(item, permissionSet, roleSet))
                    .ToList()??new List<NavigationItem>();

                // 2. Врати го комплетниот објект со СИТЕ својства (вклучувајќи ја Route за Главен Прозор)
                return new NavigationGroup
                {
                    GroupTitle=group.GroupTitle,
                    Icon=group.Icon,
                    Route=group.Route,                  // КРИТИЧНО: Мора да се префрли рутата!
                    Module=group.Module,                // КРИТИЧНО
                    RequiredPermissions=group.RequiredPermissions,
                    IsActive=group.IsActive,// КРИТИЧНО
                    Items=allowedItems
                };
            })
            .Where(g =>
                // КРИТИЧНО: Групата се прикажува ако има подменија ИЛИ ако самата таа е директен линк (како Главен Прозор)
                (g.Items!=null&&g.Items.Any())||
                (!string.IsNullOrEmpty(g.Route)&&IsGroupAllowed(g, permissionSet, roleSet))
            )
            .ToList();

        return Task.FromResult(filteredGroups);
    }

    // Проверка за посебни ставки (NavigationItem)
    private static bool IsItemAllowed(NavigationItem item, HashSet<string> userPermissions, HashSet<string> roles)
    {
        if(roles.Contains("Admin")) return true;
        if(item.RequiredPermissions==null||item.RequiredPermissions.Count==0) return true;

        return item.RequiredPermissions.All(p => userPermissions.Contains(p.ToString()));
    }

    // Проверка за главни групи (NavigationGroup - како Главен Прозор)
    private static bool IsGroupAllowed(NavigationGroup group, HashSet<string> userPermissions, HashSet<string> roles)
    {
        if(roles.Contains("Admin")) return true;
        if(group.RequiredPermissions==null||group.RequiredPermissions.Count==0) return true;

        return group.RequiredPermissions.All(p => userPermissions.Contains(p.ToString()));
    }
}