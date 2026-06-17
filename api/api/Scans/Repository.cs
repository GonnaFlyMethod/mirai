using MedScans.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MedScans.Scans;

public interface IScanRepository
{
    Task<List<BrainScan>> GetAllAsync(CancellationToken cancellationToken);
    Task<BrainScan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<BrainScan> CreateAsync(BrainScan scan, CancellationToken cancellationToken);
}

public sealed class Repository : IScanRepository
{
    private readonly AppDbContext db;

    public Repository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<BrainScan>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await db.BrainScans
            .OrderByDescending(scan => scan.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<BrainScan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.BrainScans.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<BrainScan> CreateAsync(BrainScan scan, CancellationToken cancellationToken)
    {
        db.BrainScans.Add(scan);
        await db.SaveChangesAsync(cancellationToken);
        return scan;
    }
}
