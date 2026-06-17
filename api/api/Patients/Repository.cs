using MedScans.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MedScans.Patients;

public interface IPatientRepository
{
    Task<List<Patient>> GetAllAsync();
    Task<Patient?> GetByIdAsync(Guid id);
    Task<Patient> CreateAsync(Patient patient);
    Task<bool> DeleteAsync(Guid id);
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
}
