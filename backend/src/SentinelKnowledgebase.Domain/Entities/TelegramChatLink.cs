using System.ComponentModel.DataAnnotations;

namespace SentinelKnowledgebase.Domain.Entities;

public class TelegramChatLink
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid OwnerUserId { get; set; }

    public long TelegramChatId { get; set; }

    public long TelegramUserId { get; set; }

    [MaxLength(256)]
    public string? ChatDisplayName { get; set; }

    [MaxLength(256)]
    public string? SenderDisplayName { get; set; }

    public DateTimeOffset LinkedAt { get; set; }

    public DateTimeOffset? UnlinkedAt { get; set; }
}
