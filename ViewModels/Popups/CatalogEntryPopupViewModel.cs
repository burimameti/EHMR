using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EHMR.ViewModels.Popups;

public partial class CatalogEntryPopupViewModel : ObservableObject
{
    public Action<object?>? RequestClose { get; set; }

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private bool isReadOnly;

    public bool IsEditMode => !IsReadOnly;

    public CatalogEntryPopupViewModel(string title, string name, string description, bool isReadOnly)
    {
        Title=title;
        Name=name;
        Description=description;
        IsReadOnly=isReadOnly;
    }

    [RelayCommand]
    private void Save()
    {
        if(IsReadOnly) return;
        if(string.IsNullOrWhiteSpace(Name))
            return;

        RequestClose?.Invoke(new CatalogEntryPopupResult
        {
            Confirmed=true,
            Name=Name.Trim(),
            Description=Description?.Trim() ?? string.Empty
        });
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(null);

    [RelayCommand]
    private void Close() => RequestClose?.Invoke(null);
}

public sealed class CatalogEntryPopupResult
{
    public bool Confirmed { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
