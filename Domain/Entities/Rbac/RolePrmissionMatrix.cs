using System;
using System.Collections.Generic;
using System.Linq;

namespace EHMR.Domain.Entities.Rbac;

public static class RolePermissionMatrix
{
    private static readonly IReadOnlyDictionary<UserRole, IReadOnlyDictionary<string, ModuleAction>> _templates =
        new Dictionary<UserRole, IReadOnlyDictionary<string, ModuleAction>>
        {
            [UserRole.SuperAdmin] = BuildFullTemplate(),
            [UserRole.Admin] = BuildFullTemplate(),

            [UserRole.Doctor] = Template(
                (Modules.Dashboard, ModuleAction.View),
                (Modules.Doctors, ModuleAction.View),
                (Modules.Patients, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit|ModuleAction.Activate|ModuleAction.Deactivate),
                (Modules.Appointments, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit|ModuleAction.Schedule|ModuleAction.Cancel|ModuleAction.Complete),
            
                (Modules.Protocols, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit|ModuleAction.Approve|ModuleAction.Print|ModuleAction.Export),
                (Modules.Inventory, ModuleAction.View),
                (Modules.Medicines, ModuleAction.View),
                (Modules.Reports, ModuleAction.View|ModuleAction.Export|ModuleAction.Print),
                (Modules.Calendar, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit|ModuleAction.Schedule|ModuleAction.Cancel),
                (Modules.MKBCodes, ModuleAction.View),
                (Modules.Prescriptions, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit)
            ),

            [UserRole.MainNurse] = Template(
                (Modules.Dashboard, ModuleAction.View),
                (Modules.Doctors, ModuleAction.View),
                (Modules.Patients, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit|ModuleAction.Activate|ModuleAction.Deactivate),
                (Modules.Appointments, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit|ModuleAction.Schedule|ModuleAction.Cancel|ModuleAction.Complete),
          
                (Modules.Protocols, ModuleAction.View|ModuleAction.Print),
                (Modules.Inventory, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit),
                (Modules.Medicines, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit),
                (Modules.Reports, ModuleAction.View|ModuleAction.Export|ModuleAction.Print),
                (Modules.Calendar, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit|ModuleAction.Schedule|ModuleAction.Cancel),
                (Modules.MKBCodes, ModuleAction.View),
                (Modules.Prescriptions, ModuleAction.View|ModuleAction.Create|ModuleAction.Edit)
            ),

            [UserRole.Nurse] = Template(
                (Modules.Dashboard, ModuleAction.View),
                (Modules.Doctors, ModuleAction.View),
                (Modules.Patients, ModuleAction.View|ModuleAction.Create),
                (Modules.Appointments, ModuleAction.View|ModuleAction.Create|ModuleAction.Schedule),
              
                (Modules.Protocols, ModuleAction.View|ModuleAction.Print),
                (Modules.Inventory, ModuleAction.View),
                (Modules.Medicines, ModuleAction.View),
                (Modules.Reports, ModuleAction.View),
                (Modules.Calendar, ModuleAction.View|ModuleAction.Create|ModuleAction.Schedule),
                (Modules.MKBCodes, ModuleAction.View),
                (Modules.Prescriptions, ModuleAction.View|ModuleAction.Create)
            ),

            [UserRole.Staff] = Template(
                (Modules.Dashboard, ModuleAction.View),
                (Modules.Patients, ModuleAction.View),
                (Modules.Appointments, ModuleAction.View),
               
                (Modules.Protocols, ModuleAction.View),
                (Modules.Inventory, ModuleAction.View),
                (Modules.Reports, ModuleAction.View),
                (Modules.Calendar, ModuleAction.View),
                (Modules.MKBCodes, ModuleAction.View),
                (Modules.Prescriptions, ModuleAction.View)
            )
        };

    public static ModuleAction GetActions(string module, UserRole role)
        => _templates.TryGetValue(role, out var template) &&
           template.TryGetValue(module, out var actions)
            ? actions
            : ModuleAction.None;

    public static IReadOnlyDictionary<string, ModuleAction> GetTemplate(UserRole role)
        => _templates.TryGetValue(role, out var template)
            ? template
            : new Dictionary<string, ModuleAction>(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, ModuleAction> Template(
        params (string Module, ModuleAction Actions)[] permissions)
        => permissions.ToDictionary(
            x => x.Module,
            x => x.Actions,
            StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, ModuleAction> BuildFullTemplate()
    {
        return Modules.GetAll()
            .ToDictionary(
                module => module,
                module => ModulePermissionCatalog.GetActions(module)
                    .Aggregate(ModuleAction.None, (current, action) => current|action),
                StringComparer.OrdinalIgnoreCase);
    }
}
