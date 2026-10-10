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

    /// <summary>Null when the score is entered at patient level and is not linked to an encounter.</summary>
    public Guid? EncounterId
    {
        get; set;
    }

    /// <summary>Îïèñ íà ñêîðîò (ïð. DAS28).</summary>
    public string ScoreText { get; set; } = string.Empty;

    /// <summary>Áðî¼êà (ïð. 3.2).</summary>
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
