using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRawCaptureRepository RawCaptures { get; }
    IProcessedInsightRepository ProcessedInsights { get; }
    ITagRepository Tags { get; }
    IEmbeddingVectorRepository EmbeddingVectors { get; }
    Task<int> SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    
    public IRawCaptureRepository RawCaptures { get; }
    public IProcessedInsightRepository ProcessedInsights { get; }
    public ITagRepository Tags { get; }
    public IEmbeddingVectorRepository EmbeddingVectors { get; }
    
    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        RawCaptures = new RawCaptureRepository(_context);
        ProcessedInsights = new ProcessedInsightRepository(_context);
        Tags = new TagRepository(_context);
        EmbeddingVectors = new EmbeddingVectorRepository(_context);
    }
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}
