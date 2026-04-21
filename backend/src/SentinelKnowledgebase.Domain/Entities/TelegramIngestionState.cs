using System.ComponentModel.DataAnnotations;

namespace SentinelKnowledgebase.Domain.Entities;

public class TelegramIngestionState
{
    public const int SingletonId = 1;

    [Key]
    public int Id { get; set; } = SingletonId;

    public long LastProcessedUpdateId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
