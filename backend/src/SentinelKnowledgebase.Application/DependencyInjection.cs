using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Application.Services.Telegram;
using SentinelKnowledgebase.Application.Validators;

namespace SentinelKnowledgebase.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICaptureService, CaptureService>();
        services.AddScoped<ICaptureBulkActionService, CaptureBulkActionService>();
        services.AddScoped<ICaptureProcessingAdminService, CaptureProcessingAdminService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IInsightClusteringService, InsightClusteringService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddSingleton<IMonitoringService, MonitoringService>();
        services.AddScoped<IContentProcessor, ContentProcessor>();
        services.AddHttpClient<IAssistantChatService, AssistantChatService>();
        services.AddScoped<ITelegramIntegrationService, TelegramIntegrationService>();
        
        services.AddValidatorsFromAssemblyContaining<CaptureRequestValidator>();
        services.AddValidatorsFromAssemblyContaining<SemanticSearchRequestValidator>();
        services.AddValidatorsFromAssemblyContaining<TagSearchRequestValidator>();
        
        services.AddHttpClient<IContentProcessor, ContentProcessor>();
        
        return services;
    }
}
