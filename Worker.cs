using Microsoft.Extensions.Caching.Memory;

namespace MarketPulse;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMemoryCache _cache;

    public Worker(ILogger<Worker> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}