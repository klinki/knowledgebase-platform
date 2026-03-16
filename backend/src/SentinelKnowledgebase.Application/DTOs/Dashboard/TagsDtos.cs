using System.ComponentModel.DataAnnotations;

namespace SentinelKnowledgebase.Application.DTOs.Dashboard;

public class TagRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
