using SentinelKnowledgebase.Application.DTOs.Assistant;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface IAssistantChatService
{
    Task<AssistantChatSessionDto> GetSessionAsync(Guid ownerUserId);
    Task<IReadOnlyList<AssistantChatMessageDto>> GetMessagesAsync(Guid ownerUserId);
    Task<AssistantChatMessageSendResponseDto> SendMessageAsync(Guid ownerUserId, AssistantChatMessageSendRequestDto request);
    Task<AssistantChatActionResponseDto?> ConfirmActionAsync(Guid ownerUserId, Guid actionId);
    Task<AssistantChatActionResponseDto?> CancelActionAsync(Guid ownerUserId, Guid actionId);
}
