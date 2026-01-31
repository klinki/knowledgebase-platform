namespace SentinelKnowledgebase.Domain.Entities;

public class CaptureTag
{
    public Guid RawCaptureId { get; set; }
    public RawCapture RawCapture { get; set; } = null!;
    
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
