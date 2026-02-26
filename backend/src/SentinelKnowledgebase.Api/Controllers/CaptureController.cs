using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Api.BackgroundProcessing;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.Services.Interfaces;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Route("api/v1/capture")]
public class CaptureController : ControllerBase
{
    private readonly ICaptureService _captureService;
    private readonly ICaptureProcessingQueue _captureProcessingQueue;
    private readonly ILogger<CaptureController> _logger;
    
    public CaptureController(
        ICaptureService captureService,
        ICaptureProcessingQueue captureProcessingQueue,
        ILogger<CaptureController> logger)
    {
        _captureService = captureService;
        _captureProcessingQueue = captureProcessingQueue;
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
        
        try
        {
            var response = await _captureService.CreateCaptureAsync(request);
            await _captureProcessingQueue.QueueAsync(response.Id);
            return Accepted(new CaptureAcceptedDto
            {
                Id = response.Id,
                Message = "Capture accepted and processing started"
            });
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
    [ProducesResponseType(typeof(IEnumerable<CaptureResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCaptures()
    {
        var responses = await _captureService.GetAllCapturesAsync();
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
