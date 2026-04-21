using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Api.Extensions;
using SentinelKnowledgebase.Application.DTOs.Integrations;
using SentinelKnowledgebase.Application.Services.Interfaces;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/integrations/telegram")]
public class TelegramIntegrationsController : ControllerBase
{
    private readonly ITelegramIntegrationService _telegramIntegrationService;

    public TelegramIntegrationsController(ITelegramIntegrationService telegramIntegrationService)
    {
        _telegramIntegrationService = telegramIntegrationService;
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(TelegramLinkStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TelegramLinkStatusDto>> GetStatus()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await _telegramIntegrationService.GetStatusAsync(userId));
    }

    [HttpPost("link-code")]
    [ProducesResponseType(typeof(TelegramLinkCodeResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TelegramLinkCodeResponseDto>> IssueLinkCode()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await _telegramIntegrationService.IssueLinkCodeAsync(userId));
    }

    [HttpDelete("link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unlink()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await _telegramIntegrationService.UnlinkAsync(userId);
        return NoContent();
    }
}
