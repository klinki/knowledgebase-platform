using SentinelKnowledgebase.Application.DTOs.Labels;

namespace SentinelKnowledgebase.Application.DTOs.Clusters;

public class TopicClusterSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Keywords { get; set; } = new();
    public int MemberCount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<TopicClusterRepresentativeInsightDto> RepresentativeInsights { get; set; } = new();
    public LabelAssignmentDto SuggestedLabel { get; set; } = new();
}

public class TopicClusterRepresentativeInsightDto
{
    public Guid CaptureId { get; set; }
    public Guid ProcessedInsightId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}

public class TopicClusterLinkDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LabelAssignmentDto SuggestedLabel { get; set; } = new();
}

public class TopicClusterDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Keywords { get; set; } = new();
    public int MemberCount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public LabelAssignmentDto SuggestedLabel { get; set; } = new();
    public List<TopicClusterMembershipDto> Members { get; set; } = new();
}

public class TopicClusterMembershipDto
{
    public Guid CaptureId { get; set; }
    public Guid ProcessedInsightId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public int Rank { get; set; }
    public double SimilarityToCentroid { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<LabelAssignmentDto> Labels { get; set; } = new();
}
