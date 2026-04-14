using System.ComponentModel.DataAnnotations;

namespace SentinelKnowledgebase.Domain.Entities;

public class AssistantChatSession
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid OwnerUserId { get; set; }

    public Guid? LastResultSetId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<AssistantChatMessage> Messages { get; set; } = new();

    public List<AssistantChatResultSet> ResultSets { get; set; } = new();

    public List<AssistantChatPendingAction> PendingActions { get; set; } = new();
}
