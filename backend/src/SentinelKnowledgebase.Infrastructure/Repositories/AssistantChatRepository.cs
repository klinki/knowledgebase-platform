using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class AssistantChatRepository : IAssistantChatRepository
{
    private readonly ApplicationDbContext _context;

    public AssistantChatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssistantChatSession> GetOrCreateSessionAsync(Guid ownerUserId)
    {
        var existing = await _context.AssistantChatSessions
            .FirstOrDefaultAsync(session => session.OwnerUserId == ownerUserId);
        if (existing != null)
        {
            return existing;
        }

        var session = new AssistantChatSession
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.AssistantChatSessions.Add(session);
        return session;
    }

    public async Task<AssistantChatSession?> GetByOwnerAsync(Guid ownerUserId)
    {
        return await _context.AssistantChatSessions
            .FirstOrDefaultAsync(session => session.OwnerUserId == ownerUserId);
    }

    public async Task<IReadOnlyList<AssistantChatMessage>> GetMessagesAsync(Guid ownerUserId)
    {
        return await _context.AssistantChatMessages
            .AsNoTracking()
            .Where(message => message.Session.OwnerUserId == ownerUserId)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync();
    }

    public async Task<AssistantChatResultSet?> GetResultSetByIdAsync(Guid ownerUserId, Guid resultSetId)
    {
        return await _context.AssistantChatResultSets
            .FirstOrDefaultAsync(resultSet => resultSet.Id == resultSetId && resultSet.OwnerUserId == ownerUserId);
    }

    public async Task<IReadOnlyDictionary<Guid, AssistantChatResultSet>> GetResultSetsByIdsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> resultSetIds)
    {
        if (resultSetIds.Count == 0)
        {
            return new Dictionary<Guid, AssistantChatResultSet>();
        }

        return await _context.AssistantChatResultSets
            .Where(resultSet => resultSet.OwnerUserId == ownerUserId && resultSetIds.Contains(resultSet.Id))
            .ToDictionaryAsync(resultSet => resultSet.Id);
    }

    public async Task<AssistantChatResultSet?> GetLatestResultSetAsync(Guid ownerUserId)
    {
        return await _context.AssistantChatResultSets
            .Where(resultSet => resultSet.OwnerUserId == ownerUserId)
            .OrderByDescending(resultSet => resultSet.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<AssistantChatPendingAction?> GetPendingActionAsync(Guid ownerUserId, Guid actionId)
    {
        return await _context.AssistantChatPendingActions
            .FirstOrDefaultAsync(action => action.Id == actionId && action.OwnerUserId == ownerUserId);
    }

    public async Task<IReadOnlyDictionary<Guid, AssistantChatPendingAction>> GetPendingActionsByIdsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> actionIds)
    {
        if (actionIds.Count == 0)
        {
            return new Dictionary<Guid, AssistantChatPendingAction>();
        }

        return await _context.AssistantChatPendingActions
            .Where(action => action.OwnerUserId == ownerUserId && actionIds.Contains(action.Id))
            .ToDictionaryAsync(action => action.Id);
    }

    public async Task<AssistantChatPendingAction?> GetLatestPendingActionAsync(Guid ownerUserId)
    {
        return await _context.AssistantChatPendingActions
            .Where(action =>
                action.OwnerUserId == ownerUserId &&
                action.Status == AssistantChatActionStatus.PendingConfirmation)
            .OrderByDescending(action => action.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public Task AddMessageAsync(AssistantChatMessage message)
    {
        _context.AssistantChatMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task AddResultSetAsync(AssistantChatResultSet resultSet)
    {
        _context.AssistantChatResultSets.Add(resultSet);
        return Task.CompletedTask;
    }

    public Task AddPendingActionAsync(AssistantChatPendingAction action)
    {
        _context.AssistantChatPendingActions.Add(action);
        return Task.CompletedTask;
    }

    public Task UpdateSessionAsync(AssistantChatSession session)
    {
        var entry = _context.Entry(session);
        if (entry.State == EntityState.Detached)
        {
            _context.AssistantChatSessions.Attach(session);
            _context.Entry(session).State = EntityState.Modified;
        }

        return Task.CompletedTask;
    }

    public Task UpdatePendingActionAsync(AssistantChatPendingAction action)
    {
        var entry = _context.Entry(action);
        if (entry.State == EntityState.Detached)
        {
            _context.AssistantChatPendingActions.Attach(action);
            _context.Entry(action).State = EntityState.Modified;
        }

        return Task.CompletedTask;
    }
}
