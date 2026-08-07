using EHMR.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Domain.Interfaces;

public interface IPrescriptionService
{
    /// <summary>
    /// Враќа листа од сите рецепти во системот.
    /// </summary>
    Task<IEnumerable<Prescription>> GetAllAsync();

    /// <summary>
    /// Наоѓа конкретен рецепт преку неговиот уникатен идентификатор.
    /// </summary>
    Task<Prescription?> GetByIdAsync(Guid id);

    /// <summary>
    /// Враќа колекција на рецепти препишани на конкретен пациент.
    /// </summary>
    Task<IEnumerable<Prescription>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// Враќа колекција на рецепти поврзани со конкретен медицински преглед (Encounter).
    /// </summary>
    Task<IEnumerable<Prescription>> GetByEncounterIdAsync(Guid encounterId);

    /// <summary>
    /// Креира нов рецепт во базата на податоци.
    /// </summary>
    Task<Prescription> CreateAsync(Prescription prescription);

    /// <summary>
    /// Ги ажурира информациите за постоечки рецепт.
    /// </summary>
    Task<bool> UpdateAsync(Prescription prescription);

    /// <summary>
    /// Избришува рецепт од системот (или го означува како неактивен/повлечен).
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}