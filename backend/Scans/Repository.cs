using MedScans.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MedScans.Scans;

public interface IScanRepository
{
    Task<List<BrainScan>> GetAllAsync(CancellationToken cancellationToken);
    Task<BrainScan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<BrainScan> CreateAsync(BrainScan scan, CancellationToken cancellationToken);
}

public sealed class Repository(AppDbContext db) : IScanRepository
{
    public async Task<List<BrainScan>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await db.BrainScans
            .OrderByDescending(scan => scan.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<BrainScan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.BrainScans.FindAsync([id], cancellationToken);
    }

    public async Task<BrainScan> CreateAsync(BrainScan scan, CancellationToken cancellationToken)
    {
        db.BrainScans.Add(scan);
        await db.SaveChangesAsync(cancellationToken);
        return scan;
    }
}
