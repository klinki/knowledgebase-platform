using System.ComponentModel.DataAnnotations;

namespace SentinelKnowledgebase.Domain.Entities;

public class LabelCategory
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid OwnerUserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<LabelValue> Values { get; set; } = new();
}
