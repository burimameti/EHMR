using System.Reflection;

namespace EHMR.Domain.Entities.Rbac;


[Flags]
public enum ModuleAction
{
    None = 0,
    View = 1,
    Create = 2,
    Edit = 4,
    Delete = 8,
    Full = View|Create|Edit|Delete
}
public static class Modules
{
    public const string Dashboard = "Dashboard";
   
    public const string Doctors = "Doctors";
    
    public const string BackupDashboard = "BackupDashboard";
    
    public const string Backups = "Backups";
   
    public const string Patients = "Patients";

    public const string Appointments = "Appointments";

    public const string Therapy = "Therapy";

    public const string Protocols = "Protocols";

    public const string Inventory = "Inventory";

    public const string Reports = "Reports";

    public const string Encounters = "Encounters";
    
    public const string Calendar = "Calendar";

    public const string MKBCodes = "MKBCodes";

    public const string Administration = "Administration";

    private static readonly List<string> _allModules;
  
    private static readonly Dictionary<UserRole, HashSet<string>> _roleDefaults;

    static Modules()
    {
        _allModules=new List<string>();
        _roleDefaults=Enum.GetValues<UserRole>().ToDictionary(r => r, _ => new HashSet<string>());

       
       
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