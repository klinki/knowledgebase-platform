using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class EmbeddingVectorRepository : IEmbeddingVectorRepository
{
    private readonly ApplicationDbContext _context;
    
    public EmbeddingVectorRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public Task<EmbeddingVector> AddAsync(EmbeddingVector embeddingVector)
    {
        _context.EmbeddingVectors.Add(embeddingVector);
        return Task.FromResult(embeddingVector);
    }
    
    public async Task<EmbeddingVector?> GetByProcessedInsightIdAsync(Guid processedInsightId)
    {
        return await _context.EmbeddingVectors
            .FirstOrDefaultAsync(e => e.ProcessedInsightId == processedInsightId);
    }
    
    public Task UpdateAsync(EmbeddingVector embeddingVector)
    {
        _context.EmbeddingVectors.Update(embeddingVector);
        return Task.CompletedTask;
    }
    
    public async Task DeleteAsync(Guid id)
    {
        var embeddingVector = await _context.EmbeddingVectors.FindAsync(id);
        if (embeddingVector != null)
        {
            _context.EmbeddingVectors.Remove(embeddingVector);
        }
    }
}
