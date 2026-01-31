using Microsoft.EntityFrameworkCore;
using Pgvector;
using Sentinel.Knowledgebase.Domain.Entities;
using Sentinel.Knowledgebase.Infrastructure.Data;

namespace Sentinel.Knowledgebase.Infrastructure.Repositories;

public class ProcessedInsightRepository : Repository<ProcessedInsight>, IProcessedInsightRepository
{
    public ProcessedInsightRepository(SentinelDbContext context) : base(context)
    {
    }

    public async Task<ProcessedInsight?> GetByRawCaptureIdAsync(Guid rawCaptureId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pi => pi.RawCapture)
            .FirstOrDefaultAsync(pi => pi.RawCaptureId == rawCaptureId, cancellationToken);
    }

    public async Task<IEnumerable<ProcessedInsight>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pi => pi.RawCapture)
            .Where(pi => pi.Category == category)
            .OrderByDescending(pi => pi.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProcessedInsight>> GetByTagsAsync(List<string> tags, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pi => pi.RawCapture)
            .Where(pi => pi.Tags.Any(tag => tags.Contains(tag)))
            .OrderByDescending(pi => pi.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProcessedInsight>> SemanticSearchAsync(float[] queryEmbedding, int limit = 10, float threshold = 0.7f, CancellationToken cancellationToken = default)
    {
        // For now, return recent insights with embeddings
        // TODO: Implement proper vector similarity search with pgvector
        return await _dbSet
            .Include(pi => pi.RawCapture)
            .Where(pi => pi.Embedding != null)
            .OrderByDescending(pi => pi.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
