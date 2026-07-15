using EHMR.Domain.Entities.Rbac;

namespace EHMR.Domain.Interfaces;

public interface IAuthStateService
{
    User? CurrentUser
    {
        get;
    }

    bool IsAuthenticated
    {
        get;
    }

    event EventHandler? AuthStateChanged;

    void SetUser(User user);

    void Clear();

}