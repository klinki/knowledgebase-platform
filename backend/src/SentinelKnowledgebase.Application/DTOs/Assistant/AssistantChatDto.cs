using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Application.DTOs.Assistant;

public class AssistantChatSessionDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AssistantChatMessageSendRequestDto
{
    public string Message { get; set; } = string.Empty;
}

public class AssistantChatMessageSendResponseDto
{
    public AssistantChatMessageDto UserMessage { get; set; } = new();
    public AssistantChatMessageDto AssistantMessage { get; set; } = new();
}

public class AssistantChatMessageDto
{
    public Guid Id { get; set; }
    public AssistantChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public AssistantChatResultSetDto? ResultSet { get; set; }
    public AssistantChatActionDto? Action { get; set; }
}

public class AssistantChatResultSetDto
{
    public Guid Id { get; set; }
    public string QueryType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AssistantChatResultSetItemDto> PreviewItems { get; set; } = new();
}

public class AssistantChatResultSetItemDto
{
    public Guid CaptureId { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? Status { get; set; }
    public double? Similarity { get; set; }
    public string? MatchReason { get; set; }
    public string? PreviewText { get; set; }
    public string? SkipCode { get; set; }
    public string? SkipReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssistantChatActionDto
{
    public Guid Id { get; set; }
    public AssistantChatActionType ActionType { get; set; }
    public AssistantChatActionStatus Status { get; set; }
    public Guid TargetResultSetId { get; set; }
    public int CaptureCount { get; set; }
    public int? ExecutedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
}

public class AssistantChatActionResponseDto
{
    public AssistantChatActionDto Action { get; set; } = new();
    public AssistantChatMessageDto AssistantMessage { get; set; } = new();
}
