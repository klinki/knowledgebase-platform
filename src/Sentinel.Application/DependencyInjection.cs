using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Application.Interfaces;
using Sentinel.Application.Services;

namespace Sentinel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICaptureService, CaptureService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddSingleton<ITextCleaner, TextCleaner>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
