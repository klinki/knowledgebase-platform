using System.ComponentModel.DataAnnotations;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Domain.Entities;

public class AssistantChatPendingAction
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    public AssistantChatSession Session { get; set; } = null!;

    [Required]
    public Guid OwnerUserId { get; set; }

    [Required]
    public AssistantChatActionType ActionType { get; set; }

    [Required]
    public AssistantChatActionStatus Status { get; set; }

    [Required]
    public Guid TargetResultSetId { get; set; }

    [Required]
    public string CaptureIdsJson { get; set; } = "[]";

    public int CaptureCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? ExecutedAt { get; set; }

    public int? ExecutedCount { get; set; }
}
