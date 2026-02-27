using System.Diagnostics.Metrics;
using SentinelKnowledgebase.Application.Services.Interfaces;

namespace SentinelKnowledgebase.Application.Services;

public class MonitoringService : IMonitoringService
{
    public const string MeterName = "SentinelKnowledgebase.Observability";

    private static readonly Meter Meter = new(MeterName);

    private readonly Counter<long> _processedCapturesCounter = Meter.CreateCounter<long>("capture.processed.count");
    private readonly Histogram<double> _captureProcessingDurationHistogram = Meter.CreateHistogram<double>("capture.processing.duration.ms");
    private readonly Counter<long> _aiInputTokensCounter = Meter.CreateCounter<long>("ai.tokens.input");
    private readonly Counter<long> _aiOutputTokensCounter = Meter.CreateCounter<long>("ai.tokens.output");
    private readonly Counter<long> _aiTotalTokensCounter = Meter.CreateCounter<long>("ai.tokens.total");
    private readonly Histogram<double> _embeddingGenerationLatencyHistogram = Meter.CreateHistogram<double>("ai.embedding.generation.duration.ms");

    public void IncrementProcessedCaptures()
    {
        _processedCapturesCounter.Add(1);
    }

    public void RecordCaptureProcessingDuration(double durationMilliseconds, string status)
    {
        _captureProcessingDurationHistogram.Record(
            durationMilliseconds,
            new KeyValuePair<string, object?>("status", status));
    }

    public void RecordAiTokenUsage(int inputTokens, int outputTokens, int totalTokens, string operation)
    {
        if (inputTokens > 0)
        {
            _aiInputTokensCounter.Add(inputTokens, new KeyValuePair<string, object?>("operation", operation));
        }

        if (outputTokens > 0)
        {
            _aiOutputTokensCounter.Add(outputTokens, new KeyValuePair<string, object?>("operation", operation));
        }

        if (totalTokens > 0)
        {
            _aiTotalTokensCounter.Add(totalTokens, new KeyValuePair<string, object?>("operation", operation));
        }
    }

    public void RecordEmbeddingGenerationLatency(double durationMilliseconds)
    {
        _embeddingGenerationLatencyHistogram.Record(durationMilliseconds);
    }
}
