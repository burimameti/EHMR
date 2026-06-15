using System;
using System.Collections.Generic;
using System.Linq;

using EHMR.Domain.Interfaces;

namespace EHMR.Services;

public class AuthStateService : IAuthStateService
{
    private readonly List<string> _roles = new();
    private readonly List<string> _modules = new();
    private readonly List<string> _permissions = new();
    private string _userName = string.Empty;
    private string? _token;

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_token);

    public string UserName
    {
        get => _userName;
        set
        {
            if(_userName!=value)
            {
                _userName=value;
                AuthStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public IReadOnlyList<string> Roles => _roles;
    public IReadOnlyList<string> Permissions => _permissions;
    public IReadOnlyList<string> Modules => _modules;
    public string? AccessToken => _token;

    public event EventHandler? AuthStateChanged;

    public void SetState(
     string token,
     IEnumerable<string>? roles,
     IEnumerable<string>? permissions,
     IEnumerable<string>? modules)
    {
        _token=token;

        _roles.Clear();
        _roles.AddRange(roles??Enumerable.Empty<string>());

        _permissions.Clear();
        _permissions.AddRange(permissions??Enumerable.Empty<string>());

        _modules.Clear();
        _modules.AddRange(modules??Enumerable.Empty<string>());

        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _token=null;
        _roles.Clear();
        _permissions.Clear();

        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }
}