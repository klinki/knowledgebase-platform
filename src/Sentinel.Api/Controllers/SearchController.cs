using Microsoft.AspNetCore.Mvc;
using Sentinel.Application.Dtos;
using Sentinel.Application.Interfaces;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpPost("semantic")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProcessedInsightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<ProcessedInsightDto>>> SearchSemanticAsync(
        [FromBody] SemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        var results = await _searchService.SearchSemanticAsync(request, cancellationToken);

        return Ok(results);
    }

    [HttpPost("tags")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProcessedInsightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<ProcessedInsightDto>>> SearchByTagsAsync(
        [FromBody] TagSearchRequest request,
        CancellationToken cancellationToken)
    {
        var results = await _searchService.SearchByTagsAsync(request, cancellationToken);

        return Ok(results);
    }
}
