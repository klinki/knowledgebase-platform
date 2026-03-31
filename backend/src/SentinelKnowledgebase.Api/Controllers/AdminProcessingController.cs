using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Api.Extensions;
using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Application.Services.Interfaces;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin/processing")]
public class AdminProcessingController : ControllerBase
{
    private readonly ICaptureProcessingAdminService _captureProcessingAdminService;

    public AdminProcessingController(ICaptureProcessingAdminService captureProcessingAdminService)
    {
        _captureProcessingAdminService = captureProcessingAdminService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CaptureProcessingAdminOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview()
    {
        var overview = await _captureProcessingAdminService.GetOverviewAsync();
        return Ok(overview);
    }

    [HttpPost("pause")]
    [ProducesResponseType(typeof(CaptureProcessingAdminOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pause()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var overview = await _captureProcessingAdminService.PauseAsync(userId);
        return Ok(overview);
    }

    [HttpPost("resume")]
    [ProducesResponseType(typeof(CaptureProcessingAdminOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Resume()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var overview = await _captureProcessingAdminService.ResumeAsync(userId);
        return Ok(overview);
    }
}
