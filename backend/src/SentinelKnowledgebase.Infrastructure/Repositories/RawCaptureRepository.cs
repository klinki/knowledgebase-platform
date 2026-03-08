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
    
    public Task<RawCapture> AddAsync(RawCapture rawCapture)
    {
        _context.RawCaptures.Add(rawCapture);
        return Task.FromResult(rawCapture);
    }
    
    public async Task<RawCapture?> GetByIdAsync(Guid id)
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.ProcessedInsight)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<RawCapture?> GetByIdAsync(Guid id, Guid ownerUserId)
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.ProcessedInsight)
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerUserId == ownerUserId);
    }
    
    public async Task<IEnumerable<RawCapture>> GetAllAsync(Guid ownerUserId)
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.ProcessedInsight)
            .Where(r => r.OwnerUserId == ownerUserId)
            .ToListAsync();
    }

    public async Task<IEnumerable<RawCapture>> GetRecentAsync(Guid ownerUserId, int take)
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.ProcessedInsight)
            .Where(r => r.OwnerUserId == ownerUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public Task<int> CountAsync(Guid ownerUserId)
    {
        return _context.RawCaptures.CountAsync(r => r.OwnerUserId == ownerUserId);
    }
    
    public Task UpdateAsync(RawCapture rawCapture)
    {
        _context.RawCaptures.Update(rawCapture);
        return Task.CompletedTask;
    }
    
    public async Task DeleteAsync(Guid id, Guid ownerUserId)
    {
        var rawCapture = await _context.RawCaptures
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerUserId == ownerUserId);
        if (rawCapture != null)
        {
            _context.RawCaptures.Remove(rawCapture);
        }
    }
}
