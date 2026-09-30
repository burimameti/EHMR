using System;
using System.Collections.Generic;

namespace EHMR.Domain.Entities.Rbac
{
    public static class RolePermissionMatrix
    {
        // Role defaults are the baseline. Explicit per-user module assignments
        // may override module access for an assigned module.
        private static readonly Dictionary<UserRole, ModuleAction> _roleDefaults = new()
        {
            [UserRole.SuperAdmin]=ModuleAction.Full,
            [UserRole.Admin]=ModuleAction.Full,
            [UserRole.Doctor]=ModuleAction.View|ModuleAction.Create|ModuleAction.Edit,
            [UserRole.MainNurse]=ModuleAction.View|ModuleAction.Create,
            [UserRole.Nurse]=ModuleAction.View|ModuleAction.Create,
            [UserRole.Staff]=ModuleAction.View,
        };

        private static readonly Dictionary<string, Dictionary<UserRole, ModuleAction>> _moduleOverrides
            = new(StringComparer.OrdinalIgnoreCase)
            {
                [Modules.Administration]=new()
                {
                    [UserRole.Doctor]=ModuleAction.View,
                    [UserRole.MainNurse]=ModuleAction.None,
                    [UserRole.Nurse]=ModuleAction.None,
                    [UserRole.Staff]=ModuleAction.None,
                },
                [Modules.Reports]=new()
                {
                    [UserRole.Nurse]=ModuleAction.View,
                    [UserRole.Staff]=ModuleAction.View,
                },
            };

        public static ModuleAction GetActions(string module, UserRole role)
        {
            if(_moduleOverrides.TryGetValue(module, out var overrides)&&
               overrides.TryGetValue(role, out var overrideActions))
            {
                return overrideActions;
            }

            return _roleDefaults.TryGetValue(role, out var defaults)
                ? defaults
                : ModuleAction.None;
        }
    }
}