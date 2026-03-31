using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRawCaptureRepository RawCaptures { get; }
    ICaptureProcessingControlRepository CaptureProcessingControls { get; }
    IProcessedInsightRepository ProcessedInsights { get; }
    ITagRepository Tags { get; }
    ILabelCategoryRepository LabelCategories { get; }
    ILabelValueRepository LabelValues { get; }
    IEmbeddingVectorRepository EmbeddingVectors { get; }
    Task<int> SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    
    public IRawCaptureRepository RawCaptures { get; }
    public ICaptureProcessingControlRepository CaptureProcessingControls { get; }
    public IProcessedInsightRepository ProcessedInsights { get; }
    public ITagRepository Tags { get; }
    public ILabelCategoryRepository LabelCategories { get; }
    public ILabelValueRepository LabelValues { get; }
    public IEmbeddingVectorRepository EmbeddingVectors { get; }
    
    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        RawCaptures = new RawCaptureRepository(_context);
        CaptureProcessingControls = new CaptureProcessingControlRepository(_context);
        ProcessedInsights = new ProcessedInsightRepository(_context);
        Tags = new TagRepository(_context);
        LabelCategories = new LabelCategoryRepository(_context);
        LabelValues = new LabelValueRepository(_context);
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
