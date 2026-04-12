using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Api.Extensions;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;
    
    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SearchResultPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromBody] SearchRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var results = await _searchService.SearchAsync(userId, request);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Combined search request was invalid.");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Combined search failed.");
            return StatusCode(500, "An error occurred during search");
        }
    }
    
    [HttpPost("semantic")]
    [ProducesResponseType(typeof(IEnumerable<SemanticSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SemanticSearch([FromBody] SemanticSearchRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        
        try
        {
            var results = await _searchService.SemanticSearchAsync(userId, request);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic search failed for query: {Query}", request.Query);
            return StatusCode(500, "An error occurred during semantic search");
        }
    }
    
    [HttpPost("tags")]
    [ProducesResponseType(typeof(IEnumerable<TagSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TagSearch([FromBody] TagSearchRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        
        try
        {
            var results = await _searchService.SearchByTagsAsync(userId, request);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tag search failed for tags: {Tags}", string.Join(", ", request.Tags));
            return StatusCode(500, "An error occurred during tag search");
        }
    }

    [HttpPost("labels")]
    [ProducesResponseType(typeof(IEnumerable<LabelSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LabelSearch([FromBody] LabelSearchRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var results = await _searchService.SearchByLabelsAsync(userId, request);
            return Ok(results);
        }
        catch (Exception ex)
        {
            var labels = request.Labels.Select(label => $"{label.Category}={label.Value}");
            _logger.LogError(ex, "Label search failed for labels: {Labels}", string.Join(", ", labels));
            return StatusCode(500, "An error occurred during label search");
        }
    }
}
