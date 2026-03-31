using System.ComponentModel.DataAnnotations;

namespace SentinelKnowledgebase.Domain.Entities;

public class CaptureProcessingControl
{
    public const int SingletonId = 1;

    [Key]
    public int Id { get; set; } = SingletonId;

    public bool IsPaused { get; set; }

    public DateTimeOffset? ChangedAt { get; set; }

    public Guid? ChangedByUserId { get; set; }
}
