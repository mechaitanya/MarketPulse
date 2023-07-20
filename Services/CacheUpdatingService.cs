using MarketPulse.DbContext;
using MarketPulse.Infrastructure;
using Microsoft.Extensions.Caching.Memory;

namespace MarketPulse.Services
{
    public class CacheUpdatingService : IHostedService
    {
        private Timer _timer;
        private readonly IMemoryCache _cache;
        private readonly IMyLogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRSSFeedServiceDbContextFactory _dbContextFactory;
        private string _cacheHolidaysKey;

        public CacheUpdatingService(IMemoryCache cache, IMyLogger logger, IConfiguration configuration,
            IServiceProvider serviceProvider, IRSSFeedServiceDbContextFactory dbContextFactory)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _dbContextFactory = dbContextFactory;
            _cacheHolidaysKey = "HolidaysCacheKey";
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(UpdateCache, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            return Task.CompletedTask;
        }

        public async void UpdateCache(object state)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = _dbContextFactory.CreateDbContext();
                    var instrumentIds = dbContext.Users
                                                 .Select(user => user.InstrumentId)
                                                 .ToList();
                    var stringOfInstrumentIds = string.Join(",", instrumentIds);

                    var holidays = new List<PublicHoliday>();

                    if (_cache.TryGetValue(_cacheHolidaysKey, out List<PublicHoliday> cachedHolidays))
                    {
                        holidays = cachedHolidays;
                    }
                    else
                    {
                        var accessPublicHolidayData = new AccessPublicHolidayData(_configuration, _logger);
                        holidays = await accessPublicHolidayData.SelectAllPublicHolidaysAsync(stringOfInstrumentIds);

                        TimeSpan cacheDuration = GetCacheDuration();
                        _cache.Set(_cacheHolidaysKey, holidays, cacheDuration);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + ", An error occurred while updating the cache.");
            }
        }

        private static TimeSpan GetCacheDuration()
        {
            return TimeSpan.FromDays(7);
        }
    }
}