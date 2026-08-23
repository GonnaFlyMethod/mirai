using MedScans.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MedScans.Histories;

public interface IHistoryRepository
{
    Task<HistoryRecord> CreateHistoryAsync(HistoryRecord record);
    Task<List<HistoryRecord>> CreateHistoriesAsync(List<HistoryRecord> records);
    Task<List<HistoryRecord>> SearchInHistoryAsync(Guid patientId, HistorySearchCriteria criteria);
}

public class HistoryRepository : IHistoryRepository
{
    private readonly AppDbContext db;

    public HistoryRepository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<HistoryRecord> CreateHistoryAsync(HistoryRecord record)
    {
        db.Histories.Add(record);
        await db.SaveChangesAsync();

        return record;
    }

    public async Task<List<HistoryRecord>> CreateHistoriesAsync(List<HistoryRecord> records)
    {
        db.Histories.AddRange(records);
        await db.SaveChangesAsync();

        return records;
    }

    public async Task<List<HistoryRecord>> SearchInHistoryAsync(Guid patientId, HistorySearchCriteria criteria)
    {
        var query = db.Histories.Where(record => record.PatientId == patientId);

        if (criteria.Datetime is { } datetime)
        {
            var start = datetime.Date;
            var end = start.AddDays(1);
            query = query.Where(record => record.Datetime >= start && record.Datetime < end);
        }

        var candidates = await query.ToListAsync();

        if (string.IsNullOrWhiteSpace(criteria.Title) && string.IsNullOrWhiteSpace(criteria.Description))
        {
            return candidates.OrderBy(record => record.Datetime).ToList();
        }

        return candidates
            .Select(record => (record, score: HistorySimilarity.Score(record, criteria)))
            .Where(pair => pair.score > 0)
            .OrderByDescending(pair => pair.score)
            .Select(pair => pair.record)
            .ToList();
    }
}
