using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;

namespace EHMR.Services;

public class AuthStateService : IAuthStateService
{
    private User? _currentUser;

    public User? CurrentUser => _currentUser;

    public bool IsAuthenticated => _currentUser is not null;

    public event EventHandler? AuthStateChanged;

    public void SetUser(User user)
    {
        _currentUser=user;
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _currentUser=null;
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }
}