using System;
using System.Collections.Generic;
using System.Linq;

namespace EHMR.Domain.Entities.Rbac;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// System authorization role. This is independent from any medical profile.
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Optional medical profile. Only users with UserRole.Doctor should have a Doctor profile.
    /// </summary>
    public Doctor? Doctor { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Organizational position/title. This is not an authorization role.
    /// </summary>
    public UserPosition Position { get; set; } = UserPosition.Regular;

    // Explicit per-user module assignments override the role default for that module.
    public ICollection<Module> Modules { get; set; } = new List<Module>();

    /// <summary>Explicit per-user action permissions overriding role defaults.</summary>
    public ICollection<UserModulePermission> ModulePermissions { get; set; } = new List<UserModulePermission>();

    public ICollection<UserScope> Scopes { get; set; } = new List<UserScope>();

    public bool IsAuthorizedToModule(string moduleKey)
    {
        if(!IsActive)
            return false;

        var explicitModule = Modules.FirstOrDefault(
            m => string.Equals(m.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));

        if(explicitModule!=null)
            return explicitModule.IsEnabled;

        return Rbac.Modules.GetDefaultsForRole(Role)
            .Contains(moduleKey, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> GetAuthorizedModules()
    {
        return Rbac.Modules
            .GetAll()
            .Where(IsAuthorizedToModule);
    }
}

public enum UserPosition
{
    Regular,
    Senior,
    Head,
    Primarius
}