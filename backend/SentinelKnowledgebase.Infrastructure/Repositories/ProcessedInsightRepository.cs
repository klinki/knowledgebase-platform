using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class ProcessedInsightRepository : IProcessedInsightRepository
{
    private readonly ApplicationDbContext _context;

    public ProcessedInsightRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProcessedInsight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessedInsights
            .Include(pi => pi.RawCapture)
            .FirstOrDefaultAsync(pi => pi.Id == id, cancellationToken);
    }

    public async Task<ProcessedInsight?> GetByRawCaptureIdAsync(Guid rawCaptureId, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessedInsights
            .Include(pi => pi.RawCapture)
            .FirstOrDefaultAsync(pi => pi.RawCaptureId == rawCaptureId, cancellationToken);
    }

    public async Task<ProcessedInsight> AddAsync(ProcessedInsight insight, CancellationToken cancellationToken = default)
    {
        _context.ProcessedInsights.Add(insight);
        await _context.SaveChangesAsync(cancellationToken);
        return insight;
    }

    public async Task<IEnumerable<ProcessedInsight>> SearchByEmbeddingAsync(float[] embedding, int limit = 10, CancellationToken cancellationToken = default)
    {
        var vector = new Vector(embedding);
        
        return await _context.ProcessedInsights
            .OrderBy(pi => pi.Embedding!.L2Distance(vector))
            .Take(limit)
            .Include(pi => pi.RawCapture)
            .ToListAsync(cancellationToken);
    }
}
