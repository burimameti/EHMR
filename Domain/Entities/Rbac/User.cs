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

    public UserRole Role
    {
        get; set;
    }
    public Doctor? Doctor
    {
        get; set;
    }

    public bool IsActive { get; set; } = true;

    public UserPosition Position { get; set; } = UserPosition.Regular;

    // Врската Many-to-Many со експлицитните модули запишани во базата
    public ICollection<Module> Modules { get; set; } = new List<Module>();

    public ICollection<UserScope> Scopes { get; set; } = new List<UserScope>();

    public bool IsAuthorizedToModule(string moduleKey)
    {
        if(!IsActive)
            return false;

        var explicitModule = Modules.FirstOrDefault(
            m => string.Equals(m.ModuleKey, moduleKey,
                StringComparison.OrdinalIgnoreCase));

        if(explicitModule!=null)
        {
            return explicitModule.IsEnabled;
        }

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
    Primarius,
    SuperAdmin
}