using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Application.DTOs;
using SentinelKnowledgebase.Application.Services;

namespace SentinelKnowledgebase.API.Controllers;

[ApiController]
[Route("api/v1/capture")]
public class CaptureController : ControllerBase
{
    private readonly ICaptureService _captureService;
    private readonly IProcessingService _processingService;
    private readonly IValidator<CaptureRequest> _validator;

    public CaptureController(
        ICaptureService captureService,
        IProcessingService processingService,
        IValidator<CaptureRequest> validator)
    {
        _captureService = captureService;
        _processingService = processingService;
        _validator = validator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CaptureResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCapture([FromBody] CaptureRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var response = await _captureService.CreateCaptureAsync(request, cancellationToken);
        
        // Trigger async processing
        _ = _processingService.ProcessCaptureAsync(response.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetInsight),
            new { id = response.Id },
            response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InsightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInsight(Guid id, CancellationToken cancellationToken)
    {
        var insight = await _captureService.GetInsightAsync(id, cancellationToken);
        
        if (insight == null)
        {
            return NotFound();
        }

        return Ok(insight);
    }
}
