namespace EHMR.Domain.Entities;

public class PatientScore
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public Guid EncounterId { get; set; }

    /// <summary>
    /// The clinical score/result recorded for this encounter.
    /// Kept as text so the application can support different scoring systems
    /// without forcing a numeric interpretation.
    /// </summary>
    public string ScoreText { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;

    public Encounter Encounter { get; set; } = null!;
}
