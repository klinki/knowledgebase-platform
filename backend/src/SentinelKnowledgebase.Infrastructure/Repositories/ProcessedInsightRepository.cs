using Microsoft.EntityFrameworkCore;
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
