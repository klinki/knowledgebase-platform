using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Api.Extensions;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var overview = await _dashboardService.GetOverviewAsync(userId);
        return Ok(overview);
    }
}
