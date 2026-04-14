using System.ComponentModel.DataAnnotations;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Domain.Entities;

public class AssistantChatMessage
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    public AssistantChatSession Session { get; set; } = null!;

    [Required]
    public AssistantChatMessageRole Role { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public Guid? ResultSetId { get; set; }

    public Guid? ActionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
