using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Application.Validators;

namespace SentinelKnowledgebase.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICaptureService, CaptureService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IContentProcessor, ContentProcessor>();
        
        services.AddValidatorsFromAssemblyContaining<CaptureRequestValidator>();
        services.AddValidatorsFromAssemblyContaining<SemanticSearchRequestValidator>();
        services.AddValidatorsFromAssemblyContaining<TagSearchRequestValidator>();
        
        services.AddHttpClient<IContentProcessor, ContentProcessor>();
        
        return services;
    }
}
