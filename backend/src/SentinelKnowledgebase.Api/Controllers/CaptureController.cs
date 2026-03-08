using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Api.Extensions;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/capture")]
public class CaptureController : ControllerBase
{
    private readonly ICaptureService _captureService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CaptureController> _logger;
    
    public CaptureController(
        ICaptureService captureService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<CaptureController> logger)
    {
        _captureService = captureService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(CaptureAcceptedDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCapture([FromBody] CaptureRequestDto request)
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
            var response = await _captureService.CreateCaptureAsync(userId, request);
            var jobId = _backgroundJobClient.Enqueue<ICaptureService>(service => service.ProcessCaptureAsync(response.Id));
            _logger.LogInformation(
                "Capture {CaptureId} accepted for source {SourceUrl}; Hangfire job {JobId} enqueued",
                response.Id,
                request.SourceUrl,
                jobId);

            return Accepted(new CaptureAcceptedDto
            {
                Id = response.Id,
                Message = "Capture accepted and processing enqueued"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create capture for source {SourceUrl}", request.SourceUrl);
            return StatusCode(500, "An error occurred while processing the capture");
        }
    }
    
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CaptureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCapture(Guid id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await _captureService.GetCaptureByIdAsync(userId, id);
        if (response == null)
        {
            return NotFound();
        }
        
        return Ok(response);
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CaptureResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCaptures()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var responses = await _captureService.GetAllCapturesAsync(userId);
        return Ok(responses);
    }
    
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCapture(Guid id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var deleted = await _captureService.DeleteCaptureAsync(userId, id);
            return deleted ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete capture {CaptureId}", id);
            return StatusCode(500, "An error occurred while deleting the capture");
        }
    }
}
