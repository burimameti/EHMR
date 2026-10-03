using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities.Rbac;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class ModulePermissionWrapper : ObservableObject
{
    public string ModuleKey { get; }
    public string DisplayName { get; }
    public ObservableCollection<ActionPermissionWrapper> Actions { get; } = new();

    public ModulePermissionWrapper(string moduleKey)
    {
        ModuleKey=moduleKey;
        DisplayName=moduleKey switch
        {
            Modules.Dashboard => "Почетна страна",
            Modules.Inventory => "Лекови",
            Modules.MKBCodes => "MKB-10 шифарник",
            Modules.BackupDashboard => "Backup Dashboard",
            Modules.Backups => "Резервни копии",
            Modules.Encounters => "Прегледи / Encounter",
            Modules.Administration => "Администрација",
            _ => moduleKey
        };
    }
}

public partial class ActionPermissionWrapper : ObservableObject
{
    public ModuleAction Action { get; }
    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isSelected;

    public ActionPermissionWrapper(ModuleAction action, bool isSelected)
    {
        Action=action;
        IsSelected=isSelected;
        DisplayName=action switch
        {
            ModuleAction.View => "Преглед",
            ModuleAction.Create => "Креирање",
            ModuleAction.Edit => "Уредување",
            ModuleAction.Delete => "Бришење",
            ModuleAction.Schedule => "Закажување",
            ModuleAction.Cancel => "Откажување",
            ModuleAction.Complete => "Завршување",
            ModuleAction.Approve => "Одобрување",
            ModuleAction.Export => "Извоз",
            ModuleAction.Print => "Печатење",
            ModuleAction.Manage => "Управување",
            _ => action.ToString()
        };
    }
}
