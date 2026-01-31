using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class RawCaptureRepository : IRawCaptureRepository
{
    private readonly ApplicationDbContext _context;
    
    public RawCaptureRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<RawCapture> AddAsync(RawCapture rawCapture)
    {
        _context.RawCaptures.Add(rawCapture);
        await _context.SaveChangesAsync();
        return rawCapture;
    }
    
    public async Task<RawCapture?> GetByIdAsync(Guid id)
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.ProcessedInsight)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
    
    public async Task<IEnumerable<RawCapture>> GetAllAsync()
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.ProcessedInsight)
            .ToListAsync();
    }
    
    public async Task UpdateAsync(RawCapture rawCapture)
    {
        _context.RawCaptures.Update(rawCapture);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(Guid id)
    {
        var rawCapture = await _context.RawCaptures.FindAsync(id);
        if (rawCapture != null)
        {
            _context.RawCaptures.Remove(rawCapture);
            await _context.SaveChangesAsync();
        }
    }
}
