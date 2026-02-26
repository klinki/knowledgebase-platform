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
    
    public Task<ProcessedInsight> AddAsync(ProcessedInsight processedInsight)
    {
        _context.ProcessedInsights.Add(processedInsight);
        return Task.FromResult(processedInsight);
    }
    
    public async Task<ProcessedInsight?> GetByIdAsync(Guid id)
    {
        return await _context.ProcessedInsights
            .Include(p => p.Tags)
            .Include(p => p.EmbeddingVector)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
    
    public async Task<IEnumerable<ProcessedInsight>> GetAllAsync()
    {
        return await _context.ProcessedInsights
            .Include(p => p.Tags)
            .Include(p => p.EmbeddingVector)
            .ToListAsync();
    }

    public async Task<IEnumerable<SemanticSearchRecord>> SemanticSearchAsync(float[] queryEmbedding, int topK, double threshold)
    {
        var queryVector = new Vector(queryEmbedding);

        return await _context.ProcessedInsights
            .AsNoTracking()
            .Where(p => p.EmbeddingVector != null)
            .Select(p => new SemanticSearchRecord
            {
                Id = p.Id,
                Title = p.Title,
                Summary = p.Summary,
                SourceUrl = p.RawCapture.SourceUrl,
                Similarity = 1 - p.EmbeddingVector!.Vector.CosineDistance(queryVector),
                Tags = p.Tags.Select(t => t.Name).ToList()
            })
            .Where(r => r.Similarity >= threshold)
            .OrderByDescending(r => r.Similarity)
            .Take(topK)
            .ToListAsync();
    }

    public async Task<IEnumerable<TagSearchRecord>> SearchByTagsAsync(IReadOnlyCollection<string> tags, bool matchAll)
    {
        var normalizedTags = tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct()
            .ToList();

        var query = _context.ProcessedInsights
            .AsNoTracking()
            .Where(p => p.Tags.Any(t => normalizedTags.Contains(t.Name)));

        if (matchAll)
        {
            query = query.Where(p =>
                p.Tags
                    .Where(t => normalizedTags.Contains(t.Name))
                    .Select(t => t.Name)
                    .Distinct()
                    .Count() == normalizedTags.Count);
        }

        return await query
            .Select(p => new TagSearchRecord
            {
                Id = p.Id,
                Title = p.Title,
                Summary = p.Summary,
                SourceUrl = p.RawCapture.SourceUrl,
                Tags = p.Tags.Select(t => t.Name).ToList(),
                ProcessedAt = p.ProcessedAt
            })
            .ToListAsync();
    }
    
    public Task UpdateAsync(ProcessedInsight processedInsight)
    {
        _context.ProcessedInsights.Update(processedInsight);
        return Task.CompletedTask;
    }
    
    public async Task DeleteAsync(Guid id)
    {
        var processedInsight = await _context.ProcessedInsights.FindAsync(id);
        if (processedInsight != null)
        {
            _context.ProcessedInsights.Remove(processedInsight);
        }
    }
}
