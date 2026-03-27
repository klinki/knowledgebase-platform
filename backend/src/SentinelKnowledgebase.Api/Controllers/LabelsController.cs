using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Api.Extensions;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services.Interfaces;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/labels")]
public class LabelsController : ControllerBase
{
    private readonly ILabelService _labelService;

    public LabelsController(ILabelService labelService)
    {
        _labelService = labelService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LabelCategorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var categories = await _labelService.GetCategoriesAsync(userId);
        return Ok(categories);
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(LabelCategorySummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] LabelCategoryRequestDto request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var category = await _labelService.CreateCategoryAsync(userId, request.Name);
            return CreatedAtAction(nameof(GetAll), category);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("categories/{id:guid}")]
    [ProducesResponseType(typeof(LabelCategorySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RenameCategory(Guid id, [FromBody] LabelCategoryRequestDto request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var category = await _labelService.RenameCategoryAsync(userId, id, request.Name);
            return category == null ? NotFound() : Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("categories/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted = await _labelService.DeleteCategoryAsync(userId, id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("categories/{id:guid}/values")]
    [ProducesResponseType(typeof(LabelValueSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateValue(Guid id, [FromBody] LabelValueRequestDto request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var value = await _labelService.CreateValueAsync(userId, id, request.Value);
            return value == null ? NotFound() : CreatedAtAction(nameof(GetAll), value);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("values/{id:guid}")]
    [ProducesResponseType(typeof(LabelValueSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RenameValue(Guid id, [FromBody] LabelValueRequestDto request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var value = await _labelService.RenameValueAsync(userId, id, request.Value);
            return value == null ? NotFound() : Ok(value);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("values/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteValue(Guid id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted = await _labelService.DeleteValueAsync(userId, id);
        return deleted ? NoContent() : NotFound();
    }
}
