using System;
using System.Collections.Generic;
using System.Linq;

namespace EHMR.Domain.Entities.Rbac;

public class Module : BaseEntity
{
    public Guid UserId
    {
        get; set;
    }

    public User User { get; set; } = null!;
    public string ModuleKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

// Дефинираме атрибут со кој директно кажуваме кои улоги имаат дефолтен пристап
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class DefaultRolesAttribute : Attribute
{
    public UserRole[] Roles
    {
        get;
    }

    public DefaultRolesAttribute(params UserRole[] roles) => Roles=roles;
}