using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Application.DTOs;
using SentinelKnowledgebase.Application.Services;

namespace SentinelKnowledgebase.API.Controllers;

[ApiController]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IValidator<SemanticSearchRequest> _semanticValidator;
    private readonly IValidator<TagSearchRequest> _tagValidator;

    public SearchController(
        ISearchService searchService,
        IValidator<SemanticSearchRequest> semanticValidator,
        IValidator<TagSearchRequest> tagValidator)
    {
        _searchService = searchService;
        _semanticValidator = semanticValidator;
        _tagValidator = tagValidator;
    }

    [HttpPost("semantic")]
    [ProducesResponseType(typeof(IEnumerable<SemanticSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SemanticSearch([FromBody] SemanticSearchRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _semanticValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var results = await _searchService.SemanticSearchAsync(request, cancellationToken);
        return Ok(results);
    }

    [HttpPost("tags")]
    [ProducesResponseType(typeof(IEnumerable<TagSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchByTags([FromBody] TagSearchRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _tagValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var results = await _searchService.SearchByTagAsync(request, cancellationToken);
        return Ok(results);
    }
}
