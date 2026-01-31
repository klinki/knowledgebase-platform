using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Sentinel.Application.Interfaces;
using Sentinel.Domain.Entities;
using Sentinel.Domain.Normalization;
using Sentinel.Infrastructure.Data;

namespace Sentinel.Infrastructure.Repositories;

public sealed class ProcessedInsightRepository : IProcessedInsightRepository
{
    private readonly SentinelDbContext _dbContext;

    public ProcessedInsightRepository(SentinelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(ProcessedInsight insight, CancellationToken cancellationToken)
    {
        return _dbContext.ProcessedInsights.AddAsync(insight, cancellationToken).AsTask();
    }

    public Task<ProcessedInsight?> GetByCaptureIdAsync(Guid rawCaptureId, CancellationToken cancellationToken)
    {
        return _dbContext.ProcessedInsights
            .Include(insight => insight.Tags)
            .ThenInclude(link => link.Tag)
            .AsNoTracking()
            .FirstOrDefaultAsync(insight => insight.RawCaptureId == rawCaptureId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProcessedInsight>> SearchSemanticAsync(Vector embedding, int limit, CancellationToken cancellationToken)
    {
        var vectorParameter = new NpgsqlParameter("embedding", NpgsqlDbType.Vector)
        {
            Value = embedding
        };
        var limitParameter = new NpgsqlParameter("limit", limit);

        var insights = await _dbContext.ProcessedInsights
            .FromSqlRaw(
                """
                SELECT * FROM processed_insights
                ORDER BY embedding <=> @embedding
                LIMIT @limit
                """,
                vectorParameter,
                limitParameter)
            .Include(insight => insight.Tags)
            .ThenInclude(link => link.Tag)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return insights;
    }

    public async Task<IReadOnlyCollection<ProcessedInsight>> SearchByTagsAsync(
        IReadOnlyCollection<string> tags,
        bool matchAll,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalizedTags = tags
            .Select(TagNormalizer.Normalize)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct()
            .ToArray();

        if (normalizedTags.Length == 0)
        {
            return Array.Empty<ProcessedInsight>();
        }

        var query = _dbContext.ProcessedInsights
            .Include(insight => insight.Tags)
            .ThenInclude(link => link.Tag)
            .AsQueryable();

        if (matchAll)
        {
            query = query.Where(insight =>
                insight.Tags.Count(link => normalizedTags.Contains(link.Tag.Name)) == normalizedTags.Length);
        }
        else
        {
            query = query.Where(insight => insight.Tags.Any(link => normalizedTags.Contains(link.Tag.Name)));
        }

        var results = await query
            .AsNoTracking()
            .Take(limit)
            .ToListAsync(cancellationToken);

        return results;
    }
}
