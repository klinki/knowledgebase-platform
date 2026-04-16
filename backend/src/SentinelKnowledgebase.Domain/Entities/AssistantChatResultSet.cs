using System.ComponentModel.DataAnnotations;

namespace SentinelKnowledgebase.Domain.Entities;

public class AssistantChatResultSet
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    public AssistantChatSession Session { get; set; } = null!;

    [Required]
    public Guid OwnerUserId { get; set; }

    [Required]
    [MaxLength(80)]
    public string QueryType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string CaptureIdsJson { get; set; } = "[]";

    [Required]
    public string PreviewJson { get; set; } = "[]";

    [Required]
    public string CriteriaJson { get; set; } = "{}";

    public int TotalCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
