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

public class TopicClusterListQueryDto
{
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 12;
  public string? Query { get; set; }
  public string SortField { get; set; } = "memberCount";
  public string SortDirection { get; set; } = "desc";
}

public static class TopicClusterMemberSortFields
{
    public const string Rank = "rank";
    public const string Similarity = "similarity";
    public const string Title = "title";
    public const string SourceUrl = "sourceUrl";

    public static bool IsValid(string? value)
    {
        return string.Equals(value, Rank, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, Similarity, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, Title, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, SourceUrl, StringComparison.OrdinalIgnoreCase);
    }
}

public static class SortDirections
{
    public const string Asc = "asc";
    public const string Desc = "desc";

    public static bool IsValid(string? value)
    {
        return string.Equals(value, Asc, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, Desc, StringComparison.OrdinalIgnoreCase);
    }
}

public class TopicClusterDetailQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortField { get; set; } = TopicClusterMemberSortFields.Rank;
    public string SortDirection { get; set; } = SortDirections.Asc;
}

public class TopicClusterListPageDto
{
  public List<TopicClusterSummaryDto> Items { get; set; } = new();
  public int TotalCount { get; set; }
  public int Page { get; set; }
  public int PageSize { get; set; }
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
    public int MembersPage { get; set; }
    public int MembersPageSize { get; set; }
    public int MembersTotalCount { get; set; }
    public string MembersSortField { get; set; } = TopicClusterMemberSortFields.Rank;
    public string MembersSortDirection { get; set; } = SortDirections.Asc;
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

public class ClusterRebuildAcceptedDto
{
    public string JobId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
