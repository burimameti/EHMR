//using EHMR.Domain.Entities.Rbac;
//using EHMR.Domain.Interfaces;

//namespace EHMR.Services;

//public class PolicyEngine : IPolicyEngine
//{
//    public bool HasPermission(IEnumerable<string>? userPermissions, string permission)
//    {
//        if(userPermissions==null) return false;

//        return userPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
//    }

//    public bool CanAccessModule(IEnumerable<string>? userPermissions, string module)
//    {
//        if(userPermissions==null) return false;

//        // module is derived from permissions convention: "patients.*"
//        var modulePrefix = module.ToLower() switch
//        {
//            Modules.Patients => "patients.",
//            Modules.Appointments => "appointments.",
//            Modules.Therapy => "therapy.",
//            Modules.Reports => "reports.",
//            Modules.Administration => "admin.",
//            _ => module.ToLower()+"."
//        };

//        return userPermissions.Any(p =>
//            p.StartsWith(modulePrefix, StringComparison.OrdinalIgnoreCase));
//    }

//    public IReadOnlyList<string> ResolveModules(IEnumerable<string>? userPermissions)
//    {
//        if(userPermissions==null) return [];

//        return userPermissions
//            .Select(p => p.Split('.')[0])
//            .Distinct(StringComparer.OrdinalIgnoreCase)
//            .Select(prefix => prefix switch
//            {
//                "patients" => Modules.Patients,
//                "appointments" => Modules.Appointments,
//                "therapy" => Modules.Therapy,
//                "reports" => Modules.Reports,
//                "admin" => Modules.Administration,
//                _ => prefix
//            })
//            .ToList();
//    }
//}