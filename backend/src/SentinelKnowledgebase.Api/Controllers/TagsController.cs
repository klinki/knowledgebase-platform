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

    public TagsController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
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
}
