using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.Services.Interfaces;

namespace SentinelKnowledgebase.Api.Controllers;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Route("api/v1/capture")]
[Authorize]
public class CaptureController : ControllerBase
{
    private readonly ICaptureService _captureService;
    private readonly ILogger<CaptureController> _logger;

    public CaptureController(ICaptureService captureService, ILogger<CaptureController> logger)
    {
        _captureService = captureService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CaptureResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCapture([FromBody] CaptureRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _captureService.CreateCaptureAsync(request);
            return Accepted(new { id = response.Id, message = "Capture accepted and processing started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create capture");
            return StatusCode(500, "An error occurred while processing the capture");
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CaptureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCapture(Guid id)
    {
        var response = await _captureService.GetCaptureByIdAsync(id);
        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CaptureResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCaptures([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var responses = await _captureService.GetCapturesPagedAsync(page, pageSize);
        return Ok(responses);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCapture(Guid id)
    {
        try
        {
            await _captureService.DeleteCaptureAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete capture {Id}", id);
            return NotFound();
        }
    }
}
