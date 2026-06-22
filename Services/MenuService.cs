using EHMR.Constants;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;

namespace EHMR.Services;

public class MenuService : IMenuService
{
    private readonly IAuthorizationService _auth;

    public MenuService(
        IAuthorizationService auth)
    {
        _auth=auth;
    }

    public Task<List<NavigationGroup>> UpdateMenuAsync()
    {
        var groups = AppNavigation.AllGroups
            .Select(group => new NavigationGroup
            {
                GroupTitle=group.GroupTitle,
                Icon=group.Icon,
                Route=group.Route,
                Module=group.Module,

                Items=group.Items
                    .Where(x => _auth.CanAccessModule(x.Module))
                    .Select(item => new NavigationItem
                    {
                        Title=item.Title,
                        Route=item.Route,
                        Module=item.Module,
                        Icon=item.Icon
                    })
                    .ToList()
            })
            .Where(g =>
                _auth.CanAccessModule(g.Module)
                ||g.Items.Any())
            .ToList();

        return Task.FromResult(groups);
    }

    private static bool visibleItemsExist(
        NavigationGroup group)
    {
        return group.Items?.Count>0;
    }
}