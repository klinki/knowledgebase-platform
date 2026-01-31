using Sentinel.Application.Dtos;

namespace Sentinel.Application.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyCollection<ProcessedInsightDto>> SearchSemanticAsync(SemanticSearchRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProcessedInsightDto>> SearchByTagsAsync(TagSearchRequest request, CancellationToken cancellationToken);
}
