using SentinelKnowledgebase.Application.DTOs.Search;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ISearchService
{
    Task<SearchResultPageDto> SearchAsync(Guid ownerUserId, SearchRequestDto request);
    Task<IEnumerable<SemanticSearchResultDto>> SemanticSearchAsync(Guid ownerUserId, SemanticSearchRequestDto request);
    Task<IEnumerable<TagSearchResultDto>> SearchByTagsAsync(Guid ownerUserId, TagSearchRequestDto request);
    Task<IEnumerable<LabelSearchResultDto>> SearchByLabelsAsync(Guid ownerUserId, LabelSearchRequestDto request);
}
