using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;

namespace EHMR.Services;

public class ModuleAccessService
{
    private readonly IAuthStateService _auth;

    public ModuleAccessService(IAuthStateService auth)
    {
        _auth=auth;
    }

    public bool HasModule(string moduleKey)
    {
        return _auth.Modules.Contains(moduleKey, StringComparer.OrdinalIgnoreCase);
    }
}