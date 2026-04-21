using SentinelKnowledgebase.Application.DTOs.Integrations;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ITelegramIntegrationService
{
    Task<TelegramLinkStatusDto> GetStatusAsync(Guid ownerUserId);
    Task<TelegramLinkCodeResponseDto> IssueLinkCodeAsync(Guid ownerUserId);
    Task UnlinkAsync(Guid ownerUserId);
    Task PollAndIngestAsync(CancellationToken cancellationToken);
}
