using CommunityToolkit.Maui.Views;
using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using EHMR.Services.Dto;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace EHMR.Views.Popups;

public partial class NewMedicinePopup : Popup
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private bool _isClosing;

    public NewMedicinePopup(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
    }

    public MedicineDto? MedicineResult { get; private set; }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await Shell.Current.DisplayAlert("Недостасува назив", "Внесете назив на лекот.", "ОК");
            NameEntry.Focus();
            return;
        }

        var strengthText = StrengthEntry.Text?.Trim();
        decimal strength = 0;
        if (!string.IsNullOrWhiteSpace(strengthText) &&
            !decimal.TryParse(strengthText, NumberStyles.Number, CultureInfo.CurrentCulture, out strength) &&
            !decimal.TryParse(strengthText, NumberStyles.Number, CultureInfo.InvariantCulture, out strength))
        {
            await Shell.Current.DisplayAlert("Невалидна јачина", "Јачината мора да биде број.", "ОК");
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var duplicate = await db.Medicines.AnyAsync(x => x.IsActive && x.Name.ToLower() == name.ToLower());
            if (duplicate)
            {
                await Shell.Current.DisplayAlert("Лекот постои", $"„{name}“ веќе постои во регистарот. Пребарајте го и изберете го постојниот лек.", "ОК");
                return;
            }

            var medicine = new Medicine
            {
                Id = Guid.NewGuid(),
                Name = name,
                GenericName = GenericNameEntry.Text?.Trim() ?? string.Empty,
                Code = CodeEntry.Text?.Trim() ?? string.Empty,
                DosageForm = DosageFormEntry.Text?.Trim() ?? string.Empty,
                Strength = strength,
                Unit = UnitEntry.Text?.Trim() ?? string.Empty,
                DefaultDosage = DefaultDosageEntry.Text?.Trim() ?? string.Empty,
                Manufacturer = ManufacturerEntry.Text?.Trim() ?? string.Empty,
                IsActive = true
            };
            db.Medicines.Add(medicine);
            await db.SaveChangesAsync();

            MedicineResult = new MedicineDto
            {
                Id = medicine.Id,
                Name = medicine.Name,
                GenericName = medicine.GenericName,
                Code = medicine.Code,
                DosageForm = medicine.DosageForm,
                Strength = medicine.Strength,
                Unit = medicine.Unit,
                DefaultDosage = medicine.DefaultDosage,
                Manufacturer = medicine.Manufacturer,
                IsActive = medicine.IsActive
            };
            await ClosePopupAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Грешка", $"Лекот не може да се зачува: {ex.Message}", "ОК");
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