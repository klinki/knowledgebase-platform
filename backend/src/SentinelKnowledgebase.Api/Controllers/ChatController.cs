using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Api.Extensions;
using SentinelKnowledgebase.Application.DTOs.Assistant;
using SentinelKnowledgebase.Application.Services.Interfaces;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/chat")]
public class ChatController : ControllerBase
{
    private readonly IAssistantChatService _assistantChatService;

    public ChatController(IAssistantChatService assistantChatService)
    {
        _assistantChatService = assistantChatService;
    }

    [HttpGet("session")]
    [ProducesResponseType(typeof(AssistantChatSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSession()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var session = await _assistantChatService.GetSessionAsync(userId);
        return Ok(session);
    }

    [HttpGet("session/messages")]
    [ProducesResponseType(typeof(IEnumerable<AssistantChatMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var messages = await _assistantChatService.GetMessagesAsync(userId);
        return Ok(messages);
    }

    [HttpPost("session/messages")]
    [ProducesResponseType(typeof(AssistantChatMessageSendResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage([FromBody] AssistantChatMessageSendRequestDto request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _assistantChatService.SendMessageAsync(userId, request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("actions/{actionId:guid}/confirm")]
    [ProducesResponseType(typeof(AssistantChatActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAction(Guid actionId)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await _assistantChatService.ConfirmActionAsync(userId, actionId);
        return response == null ? NotFound() : Ok(response);
    }

    [HttpPost("actions/{actionId:guid}/cancel")]
    [ProducesResponseType(typeof(AssistantChatActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelAction(Guid actionId)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await _assistantChatService.CancelActionAsync(userId, actionId);
        return response == null ? NotFound() : Ok(response);
    }
}
