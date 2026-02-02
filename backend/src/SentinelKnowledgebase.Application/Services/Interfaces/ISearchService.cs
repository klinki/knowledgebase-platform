using SentinelKnowledgebase.Application.DTOs.Search;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ISearchService
{
    Task<IEnumerable<SemanticSearchResultDto>> SemanticSearchAsync(SemanticSearchRequestDto request);
    Task<IEnumerable<TagSearchResultDto>> SearchByTagsAsync(TagSearchRequestDto request);
}
