using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class CaptureProcessingControlRepository : ICaptureProcessingControlRepository
{
    private readonly ApplicationDbContext _context;

    public CaptureProcessingControlRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CaptureProcessingControl?> GetAsync()
    {
        return await _context.CaptureProcessingControls
            .FirstOrDefaultAsync(item => item.Id == CaptureProcessingControl.SingletonId);
    }

    public Task<CaptureProcessingControl> AddAsync(CaptureProcessingControl control)
    {
        _context.CaptureProcessingControls.Add(control);
        return Task.FromResult(control);
    }

    public Task UpdateAsync(CaptureProcessingControl control)
    {
        _context.CaptureProcessingControls.Update(control);
        return Task.CompletedTask;
    }

    public Task<string?> GetDisplayNameAsync(Guid userId)
    {
        return _context.Users
            .Where(user => user.Id == userId)
            .Select(user => user.DisplayName)
            .SingleOrDefaultAsync();
    }
}
