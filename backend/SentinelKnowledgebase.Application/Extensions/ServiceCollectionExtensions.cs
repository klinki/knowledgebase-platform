using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Validators;

namespace SentinelKnowledgebase.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<ICaptureService, CaptureService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IProcessingService, ProcessingService>();

        // Register validators
        services.AddValidatorsFromAssemblyContaining<CaptureRequestValidator>();

        return services;
    }
}
