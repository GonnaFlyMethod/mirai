using MedScans.Histories;
using MedScans.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MedScans.Patients;

public interface IPatientRepository
{
    Task<List<Patient>> GetAllAsync();
    Task<Patient?> GetByIdAsync(Guid id);
    Task<Patient> CreateAsync(Patient patient);
    Task<bool> DeleteAsync(Guid id);

    Task<HistoryRecord> CreateHistoryAsync(HistoryRecord record);
    Task<List<HistoryRecord>> CreateHistoriesAsync(List<HistoryRecord> records);
    Task<List<HistoryRecord>> GetHistoryAsync(Guid patientId);
    Task<List<HistoryRecord>> SearchInHistoryAsync(Guid patientId, HistorySearchCriteria criteria);
}

public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext db;

    public PatientRepository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<Patient>> GetAllAsync()
    {
        return await db.Patients
            .OrderBy(patient => patient.LastName)
            .ThenBy(patient => patient.FirstName)
            .ToListAsync();
    }

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        return await db.Patients.FindAsync(id);
    }

    public async Task<Patient> CreateAsync(Patient patient)
    {
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        return patient;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var patient = await db.Patients.FindAsync(id);

        if (patient is null)
        {
            return false;
        }

        db.Patients.Remove(patient);
        await db.SaveChangesAsync();

        return true;
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

    public async Task<List<HistoryRecord>> GetHistoryAsync(Guid patientId)
    {
        return await db.Histories
            .Where(record => record.PatientId == patientId)
            .OrderBy(record => record.Datetime)
            .ToListAsync();
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
