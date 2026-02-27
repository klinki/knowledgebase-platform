namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface IMonitoringService
{
    void IncrementProcessedCaptures();
    void RecordCaptureProcessingDuration(double durationMilliseconds, string status);
    void RecordAiTokenUsage(int inputTokens, int outputTokens, int totalTokens, string operation);
    void RecordEmbeddingGenerationLatency(double durationMilliseconds);
}
