using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;

namespace EHMR.Services;

public class AuthStateService : IAuthStateService
{

    private User? _currentUser;
    private Guid? _currentDoctorId;

    public User? CurrentUser => _currentUser;
    public Guid? CurrentDoctorId => _currentDoctorId;
    public bool IsAuthenticated => _currentUser is not null;
    public event EventHandler? AuthStateChanged;


    public void SetUser(User user, Guid? doctorId = null)
    {
        _currentUser=user;
        _currentDoctorId=doctorId;
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }


    public void Clear()
    {
        _currentUser=null;
        _currentDoctorId=null;
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }
}