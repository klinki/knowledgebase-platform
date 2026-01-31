namespace Sentinel.Knowledgebase.Domain.Enums;

public enum CaptureStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public enum ContentType
{
    Text = 0,
    Link = 1,
    Image = 2,
    Code = 3,
    Mixed = 4
}

public enum ProcessingStatus
{
    NotStarted = 0,
    ExtractingInsights = 1,
    GeneratingEmbedding = 2,
    Completed = 3,
    Failed = 4
}
