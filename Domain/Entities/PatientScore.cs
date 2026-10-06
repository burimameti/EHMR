using System.ComponentModel.DataAnnotations.Schema;

namespace EHMR.Domain.Entities;

public class PatientScore
{
    public Guid Id
    {
        get; set;
    }

    public Guid PatientId
    {
        get; set;
    }

    /// <summary>Null кога скорот е внесен од пациентската форма (не е врзан за преглед).</summary>
    public Guid EncounterId
    {
        get; set;
    }= Guid.Empty;

    /// <summary>Опис на скорот (пр. DAS28).</summary>
    public string ScoreText { get; set; } = string.Empty;

    /// <summary>Бројка (пр. 3.2).</summary>
    public string Number { get; set; } = string.Empty;

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;

    public Encounter? Encounter
    {
        get; set;
    }

    [NotMapped]
    public string DisplayText => string.IsNullOrWhiteSpace(Number) ? ScoreText : $"{ScoreText}: {Number}";
}
