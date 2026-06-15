using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EHMR.Domain.Entities;

public class UserModule : BaseEntity
{
    public Guid UserId
    {
        get; set;
    }

    public User User { get; set; } = null!;
    public string ModuleKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

// Дефинираме атрибут со кој директно кажуваме кои улоги имаат дефолтен пристап
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class DefaultRolesAttribute : Attribute
{
    public UserRole[] Roles
    {
        get;
    }

    public DefaultRolesAttribute(params UserRole[] roles) => Roles=roles;
}

public static class Modules
{
    [DefaultRoles(UserRole.Admin, UserRole.Doctor, UserRole.MainNurse, UserRole.RegularNurse)]
    public const string Dashboard = "Dashboard";

    [DefaultRoles(UserRole.Admin, UserRole.Doctor, UserRole.MainNurse, UserRole.RegularNurse)]
    public const string Patients = "Patients";

    [DefaultRoles(UserRole.Admin, UserRole.Doctor, UserRole.MainNurse, UserRole.RegularNurse)]
    public const string Appointments = "Appointments";

    [DefaultRoles(UserRole.Admin, UserRole.Doctor, UserRole.MainNurse, UserRole.RegularNurse)]
    public const string Therapy = "Therapy";

    [DefaultRoles(UserRole.Admin, UserRole.Doctor)]
    public const string Protocols = "Protocols";

    [DefaultRoles(UserRole.Admin, UserRole.Doctor, UserRole.MainNurse)]
    public const string Inventory = "Inventory";

    [DefaultRoles(UserRole.Admin, UserRole.Doctor, UserRole.MainNurse)]
    public const string Reports = "Reports";

    [DefaultRoles(UserRole.Admin)]
    public const string Administration = "Administration";

    // --- НАПРЕДЕН ИНТЕЛИГЕНТЕН ПОГОН ---

    private static readonly List<string> _allModules;
    private static readonly Dictionary<UserRole, HashSet<string>> _roleDefaults;

    static Modules()
    {
        _allModules=new List<string>();
        _roleDefaults=Enum.GetValues<UserRole>().ToDictionary(r => r, _ => new HashSet<string>());

        // Со чист Reflection ги извлекуваме сите константи и нивните атрибути одеднаш
        var fields = typeof(Modules).GetFields(BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy)
                                    .Where(f => f.IsLiteral&&!f.IsInitOnly&&f.FieldType==typeof(string));

        foreach(var field in fields)
        {
            if(field.GetRawConstantValue() is string value)
            {
                _allModules.Add(value);

                var attr = field.GetCustomAttribute<DefaultRolesAttribute>();
                if(attr!=null)
                {
                    foreach(var role in attr.Roles)
                    {
                        _roleDefaults[role].Add(value);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Враќа листа од сите постоечки модули во системот (за полнење на CheckBox листи)
    /// </summary>
    public static IReadOnlyList<string> GetAll() => _allModules;

    /// <summary>
    /// Враќа кои модули треба автоматски да се штиклираат кога ќе се избере одредена улога
    /// </summary>
    public static IEnumerable<string> GetDefaultsForRole(UserRole role)
        => _roleDefaults.TryGetValue(role, out var mods) ? mods : Enumerable.Empty<string>();
}