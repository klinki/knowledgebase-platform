using Sentinel.Application.Models;

namespace Sentinel.Application.Interfaces;

public interface IInsightExtractionService
{
    Task<InsightExtractionResult> ExtractAsync(string cleanText, CancellationToken cancellationToken);
}
