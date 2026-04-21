using Microsoft.Extensions.Options;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Application.Services.Telegram;

namespace SentinelKnowledgebase.Worker;

public sealed class TelegramPollingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramIntegrationOptions _options;
    private readonly ILogger<TelegramPollingHostedService> _logger;

    public TelegramPollingHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<TelegramIntegrationOptions> options,
        ILogger<TelegramPollingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _logger.LogInformation("Telegram polling is disabled because no bot token is configured.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ITelegramIntegrationService>();
                await service.PollAndIngestAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Telegram polling cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollCadenceSeconds), stoppingToken);
        }
    }
}
