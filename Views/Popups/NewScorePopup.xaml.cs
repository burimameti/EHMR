using CommunityToolkit.Maui.Views;
using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Views.Popups;

public partial class NewScorePopup : Popup
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private bool _isClosing;

    public NewScorePopup(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
    }

    public string? ScoreNameResult { get; private set; }
    public string ScoreDescriptionResult { get; private set; } = string.Empty;

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await Shell.Current.DisplayAlert("Недостасува назив", "Внесете назив на скорот.", "ОК");
            NameEntry.Focus();
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var duplicate = await db.Set<ClinicalScoreDefinition>()
                .AnyAsync(x => x.IsActive && x.Name.ToLower() == name.ToLower());
            if (duplicate)
            {
                await Shell.Current.DisplayAlert("Скорот постои", $"„{name}“ веќе постои во регистарот. Пребарајте го и изберете го постојниот скор.", "ОК");
                return;
            }

            var description = DescriptionEntry.Text?.Trim() ?? string.Empty;
            db.Set<ClinicalScoreDefinition>().Add(new ClinicalScoreDefinition
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                IsActive = true
            });
            await db.SaveChangesAsync();

            ScoreNameResult = name;
            ScoreDescriptionResult = description;
            await ClosePopupAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Грешка", $"Скорот не може да се зачува: {ex.Message}", "ОК");
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e) => await ClosePopupAsync();

    private async Task ClosePopupAsync()
    {
        if (_isClosing) return;
        _isClosing = true;
#if WINDOWS
        await CloseAsync(CancellationToken.None);
#else
        await CloseAsync();
#endif
    }
}