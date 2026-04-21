using System.ComponentModel.DataAnnotations;

namespace SentinelKnowledgebase.Domain.Entities;

public class TelegramLinkCode
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid OwnerUserId { get; set; }

    [Required]
    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }
}
