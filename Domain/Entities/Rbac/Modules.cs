using System.Reflection;

namespace EHMR.Domain.Entities.Rbac;

[Flags]
public enum ModuleAction
{
    None = 0,

    // Core permissions — kept stable for existing authorization checks.
    View = 1,
    Create = 2,
    Edit = 4,
    Delete = 8,

    // Appointment-specific actions.
    Schedule = 16,
    Cancel = 32,
    Complete = 64,

    // Workflow / administration actions.
    Approve = 128,
    Export = 256,
    Print = 512,
    Manage = 1024,

    // Patient lifecycle actions.
    Activate = 2048,
    Deactivate = 4096,

    // Backward-compatible CRUD full access.
    Full = View|Create|Edit|Delete
}

/// <summary>
/// Central definition of the actions that can be assigned to each module.
/// Persistence and user-specific overrides are introduced in later phases.
/// </summary>
public static class ModulePermissionCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<ModuleAction>> _actions =
        new Dictionary<string, IReadOnlyList<ModuleAction>>(StringComparer.OrdinalIgnoreCase)
        {
            [Modules.Dashboard] = [ModuleAction.View],
            [Modules.Doctors] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete],
            [Modules.Patients] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete, ModuleAction.Activate, ModuleAction.Deactivate],
            [Modules.Appointments] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete, ModuleAction.Schedule, ModuleAction.Cancel, ModuleAction.Complete],
            [Modules.Therapy] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete],
            [Modules.Protocols] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete, ModuleAction.Approve, ModuleAction.Print, ModuleAction.Export],
            [Modules.Inventory] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete, ModuleAction.Export],
            [Modules.Reports] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Export, ModuleAction.Print],
            [Modules.Encounters] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete, ModuleAction.Approve, ModuleAction.Print, ModuleAction.Export],
            [Modules.Calendar] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete, ModuleAction.Schedule, ModuleAction.Cancel],
            [Modules.MKBCodes] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete, ModuleAction.Export, ModuleAction.Manage],
            [Modules.Administration] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete, ModuleAction.Manage],
            [Modules.BackupDashboard] = [ModuleAction.View],
            [Modules.Backups] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Delete, ModuleAction.Export, ModuleAction.Manage],
            [Modules.Prescriptions] = [ModuleAction.View, ModuleAction.Create, ModuleAction.Edit, ModuleAction.Delete]
        };

    public static IReadOnlyList<ModuleAction> GetActions(string module)
        => _actions.TryGetValue(module, out var actions)
            ? actions
            : Array.Empty<ModuleAction>();

    public static bool Supports(string module, ModuleAction action)
        => GetActions(module).Contains(action);
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
    public const string Prescriptions = "Prescriptions";

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
                        _roleDefaults[role].Add(value);
                }
            }
        }
    }

    public static IReadOnlyList<string> GetAll() => _allModules;

    public static IEnumerable<string> GetDefaultsForRole(UserRole role)
        => _roleDefaults.TryGetValue(role, out var mods)
            ? mods
            : Enumerable.Empty<string>();
}
