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

public enum AssistantChatMessageRole
{
    User,
    Assistant
}

public enum AssistantChatActionType
{
    DeleteCaptures
}

public enum AssistantChatActionStatus
{
    PendingConfirmation,
    Cancelled,
    Executed
}
