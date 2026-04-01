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
    private const string AcceptedAndEnqueuedMessage = "Capture accepted and processing enqueued";
    private const string AcceptedWhilePausedMessage = "Capture accepted; processing is currently paused";
    private const string RetryAcceptedAndEnqueuedMessage = "Capture retry accepted and processing enqueued";
    private const string RetryAcceptedWhilePausedMessage = "Capture retry accepted; processing is currently paused";

    private readonly ICaptureService _captureService;
    private readonly ICaptureProcessingAdminService _captureProcessingAdminService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CaptureController> _logger;
    
    public CaptureController(
        ICaptureService captureService,
        ICaptureProcessingAdminService captureProcessingAdminService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<CaptureController> logger)
    {
        _captureService = captureService;
        _captureProcessingAdminService = captureProcessingAdminService;
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
            var isPaused = await _captureProcessingAdminService.IsPausedAsync();
            if (!isPaused)
            {
                var jobId = _backgroundJobClient.Enqueue<ICaptureService>(service => service.ProcessCaptureAsync(response.Id));
                _logger.LogInformation(
                    "Capture {CaptureId} accepted for source {SourceUrl}; Hangfire job {JobId} enqueued",
                    response.Id,
                    request.SourceUrl,
                    jobId);
            }
            else
            {
                _logger.LogInformation(
                    "Capture {CaptureId} accepted for source {SourceUrl} while processing is paused",
                    response.Id,
                    request.SourceUrl);
            }

            return Accepted(new CaptureAcceptedDto
            {
                Id = response.Id,
                Message = isPaused ? AcceptedWhilePausedMessage : AcceptedAndEnqueuedMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create capture for source {SourceUrl}", request.SourceUrl);
            return StatusCode(500, "An error occurred while processing the capture");
        }
    }

    [HttpPost("bulk")]
    [ProducesResponseType(typeof(IEnumerable<CaptureAcceptedDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCaptures([FromBody] List<CaptureRequestDto>? requests)
    {
        if (requests == null)
        {
            return BadRequest("Request body is required.");
        }

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
            var responses = await _captureService.CreateCapturesAsync(userId, requests);
            var isPaused = await _captureProcessingAdminService.IsPausedAsync();
            var accepted = new List<CaptureAcceptedDto>(responses.Count);

            foreach (var response in responses)
            {
                accepted.Add(new CaptureAcceptedDto
                {
                    Id = response.Id,
                    Message = isPaused ? AcceptedWhilePausedMessage : AcceptedAndEnqueuedMessage
                });
            }

            if (!isPaused)
            {
                foreach (var response in responses)
                {
                    _backgroundJobClient.Enqueue<ICaptureService>(service => service.ProcessCaptureAsync(response.Id));
                }
            }

            _logger.LogInformation(
                "Accepted {CaptureCount} captures through bulk capture creation{PausedSuffix}",
                accepted.Count,
                isPaused ? " while processing is paused" : string.Empty);

            return Accepted(accepted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create {CaptureCount} captures through bulk capture creation", requests.Count);
            return StatusCode(500, "An error occurred while processing the captures");
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

    [HttpGet("list")]
    [ProducesResponseType(typeof(CaptureListPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCaptureList([FromQuery] CaptureListQueryDto query)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _captureService.GetCaptureListPageAsync(userId, query);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
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
    

    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryCapture(Guid id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var retried = await _captureService.RetryCaptureAsync(userId, id);
        if (!retried)
        {
            return NotFound();
        }

        var isPaused = await _captureProcessingAdminService.IsPausedAsync();
        if (!isPaused)
        {
            var jobId = _backgroundJobClient.Enqueue<ICaptureService>(service => service.ProcessCaptureAsync(id));
            _logger.LogInformation(
                "Capture {CaptureId} retry requested; Hangfire job {JobId} enqueued",
                id,
                jobId);
        }
        else
        {
            _logger.LogInformation(
                "Capture {CaptureId} retry requested while processing is paused",
                id);
        }

        return Accepted(new { message = isPaused ? RetryAcceptedWhilePausedMessage : RetryAcceptedAndEnqueuedMessage });
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
