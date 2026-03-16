using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Api.Extensions;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tags")]
public class TagsController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ITagService _tagService;

    public TagsController(IDashboardService dashboardService, ITagService tagService)
    {
        _dashboardService = dashboardService;
        _tagService = tagService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TagSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var tags = await _dashboardService.GetTagSummariesAsync(userId);
        return Ok(tags);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TagSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] TagRequestDto request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var tag = await _tagService.CreateTagAsync(userId, request.Name);
            return CreatedAtAction(nameof(GetAll), tag);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(TagSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Rename(Guid id, [FromBody] TagRequestDto request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var tag = await _tagService.RenameTagAsync(userId, id, request.Name);
            if (tag == null)
            {
                return NotFound();
            }

            return Ok(tag);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted = await _tagService.DeleteTagAsync(userId, id);
        return deleted ? NoContent() : NotFound();
    }
}
