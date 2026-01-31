using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class RawCaptureRepository : IRawCaptureRepository
{
    private readonly ApplicationDbContext _context;

    public RawCaptureRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RawCapture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RawCaptures
            .Include(rc => rc.ProcessedInsight)
            .Include(rc => rc.Tags)
            .FirstOrDefaultAsync(rc => rc.Id == id, cancellationToken);
    }

    public async Task<RawCapture> AddAsync(RawCapture capture, CancellationToken cancellationToken = default)
    {
        _context.RawCaptures.Add(capture);
        await _context.SaveChangesAsync(cancellationToken);
        return capture;
    }

    public async Task UpdateAsync(RawCapture capture, CancellationToken cancellationToken = default)
    {
        _context.RawCaptures.Update(capture);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<RawCapture>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RawCaptures
            .Where(rc => rc.Status == CaptureStatus.Pending)
            .ToListAsync(cancellationToken);
    }
}
