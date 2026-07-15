using System;
using System.Collections.Generic;
using System.Linq;

namespace EHMR.Domain.Entities.Rbac;

public static class RoleHierarchy
{
    // Повисок број = повисоко ниво на овластување
    private static readonly Dictionary<UserRole, int> _rank = new()
    {
        [UserRole.SuperAdmin]=100,
        [UserRole.Admin]=80,
        [UserRole.Doctor]=60,
        [UserRole.MainNurse]=40,
        [UserRole.Nurse]=20,
        [UserRole.Staff]=10,
    };

    public static int RankOf(UserRole role)
        => _rank.TryGetValue(role, out var r) ? r : 0;

    /// <summary>
    /// Дали "actor" смее да управува (edit/delete/промена на рола) со корисник со улога "target".
    /// SuperAdmin управува со сите. Admin управува со сите освен со друг SuperAdmin.
    /// Секој друг само со построго пониски рангови (не смее со себе-рамни/повисоки).
    /// </summary>
    public static bool CanManage(UserRole actor, UserRole target)
    {
        if(actor==UserRole.SuperAdmin)
            return true;

        if(actor==UserRole.Admin)
            return target!=UserRole.SuperAdmin;

        return RankOf(actor)>RankOf(target);
    }

    /// <summary>
    /// Кои улоги смее "actor" да ги додели на друг корисник (при креирање/уредување рола).
    /// </summary>
    public static IEnumerable<UserRole> AssignableRolesFor(UserRole actor)
    {
        if(actor==UserRole.SuperAdmin)
            return Enum.GetValues<UserRole>();

        if(actor==UserRole.Admin)
            return Enum.GetValues<UserRole>().Where(r => r!=UserRole.SuperAdmin);

        return Enum.GetValues<UserRole>().Where(r => RankOf(r)<RankOf(actor));
    }
}