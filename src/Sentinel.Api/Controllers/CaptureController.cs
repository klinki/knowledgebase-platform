using Microsoft.AspNetCore.Mvc;
using Sentinel.Application.Dtos;
using Sentinel.Application.Interfaces;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("api/v1/capture")]
public sealed class CaptureController : ControllerBase
{
    private readonly ICaptureService _captureService;

    public CaptureController(ICaptureService captureService)
    {
        _captureService = captureService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CaptureResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CaptureResponse>> CaptureAsync(
        [FromBody] CaptureRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _captureService.CaptureAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetCaptureAsync), new { id = response.CaptureId }, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CaptureDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaptureDetailsResponse>> GetCaptureAsync(Guid id, CancellationToken cancellationToken)
    {
        var capture = await _captureService.GetCaptureAsync(id, cancellationToken);

        if (capture is null)
        {
            return NotFound();
        }

        return Ok(capture);
    }
}
