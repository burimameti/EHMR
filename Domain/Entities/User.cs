using System;
using System.Collections.Generic;
using System.Linq;

namespace EHMR.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Складирање на примарната системска улога
    public UserRole Role
    {
        get; set;
    }

    public bool IsActive { get; set; } = true;

    // Изолација на податоци во рамките на клиниката (Scopes)
    public ICollection<UserScope> Scopes { get; set; } = new List<UserScope>();

    public UserPosition Position { get; set; } = UserPosition.Regular;

    // Врската Many-to-Many со експлицитните модули запишани во базата
    public ICollection<UserModule> Modules { get; set; } = new List<UserModule>();

    /// <summary>
    /// Напредна инстантна евалуација на авторизацијата за одреден екран (модул).
    /// Прво проверува дали има кориснички-специфична модификација во базата,
    /// а ако нема, се потпира на дефолтните улоги извлечени преку Reflection од атрибутите.
    /// </summary>
    public bool IsAuthorizedToModule(string moduleKey)
    {
        if(!IsActive) return false;

        // 1. Проверка на експлицитни дескриптори за модули во базата на корисникот
        var explicitModule = Modules.FirstOrDefault(m => string.Equals(m.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));
        if(explicitModule!=null)
        {
            return explicitModule.IsEnabled;
        }

        // 2. Fallback механизам: Ако нема ништо во базата, пресметај ги дефолтните права за улогата преку Атрибутите
        return Domain.Entities.Modules.GetDefaultsForRole(Role)
            .Contains(moduleKey, StringComparer.OrdinalIgnoreCase);
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