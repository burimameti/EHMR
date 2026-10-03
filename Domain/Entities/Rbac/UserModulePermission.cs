using System;
using EHMR.Domain.Entities.Rbac;

namespace EHMR.Domain.Entities.Rbac;

/// <summary>
/// Explicit per-user permissions for a module.
/// A row is an override: Actions can also be None to explicitly deny all actions.
/// </summary>
public class UserModulePermission : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string ModuleKey { get; set; } = string.Empty;

    public ModuleAction Actions { get; set; } = ModuleAction.None;
}
