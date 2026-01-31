using Sentinel.Application.Dtos;
using Sentinel.Domain.Entities;

namespace Sentinel.Application.Mapping;

public static class ProcessedInsightMapper
{
    public static ProcessedInsightDto ToDto(ProcessedInsight insight)
    {
        return new ProcessedInsightDto
        {
            InsightId = insight.Id,
            RawCaptureId = insight.RawCaptureId,
            Summary = insight.Summary,
            Insight = insight.Insight,
            Sentiment = insight.Sentiment,
            CleanText = insight.CleanText,
            Tags = insight.Tags.Select(link => link.Tag.Name).ToArray(),
            CreatedAt = insight.CreatedAt
        };
    }
}
