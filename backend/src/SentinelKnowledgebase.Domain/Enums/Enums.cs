namespace SentinelKnowledgebase.Domain.Enums;

public enum CaptureStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public enum ContentType
{
    Tweet,
    Article,
    Code,
    Note,
    Other
}
