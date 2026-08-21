namespace MedScans.Histories;

public sealed class HistoryRecord
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public DateTime Datetime { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public static HistoryRecord Create(Guid patientId, DateTime datetime, string title, string description)
    {
        title = title.Trim();
        description = description.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("History record title is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("History record description is required.");
        }

        return new HistoryRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Datetime = datetime,
            Title = title,
            Description = description
        };
    }
}

public sealed record HistorySearchCriteria(DateTime? Datetime, string? Title, string? Description);
