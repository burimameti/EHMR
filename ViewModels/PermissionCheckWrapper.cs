using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

/// <summary>
/// Објект кој служи за чист и реактивен MVVM Binding со CheckBox елементите во екранот за уредување.
/// </summary>
public partial class PermissionCheckWrapper : ObservableObject
{
    public string PermissionValue
    {
        get;
    }

    public string DisplayName
    {
        get;
    }

    [ObservableProperty]
    private bool _isSelected;

    public PermissionCheckWrapper(string permissionValue, bool isSelected)
    {
        PermissionValue=permissionValue;
        _isSelected=isSelected;
        DisplayName=FormatPermissionName(permissionValue);
    }

    private static string FormatPermissionName(string value)
    {
        if(string.IsNullOrWhiteSpace(value)) return string.Empty;

        // Претвора "Patients.Create" во "Patients › Create" за подобар визуелен приказ
        return value.Replace(".", " › ");
    }
}