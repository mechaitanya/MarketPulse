using MarketPulse.Infrastructure;
using MarketPulse.Models;
using static MarketPulse.Infrastructure.AccessInstrumentData;

namespace MarketPulse.RepositoryModels
{
    internal class InstrumentDataRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ITweetProperties _properties;

        public InstrumentDataRepository(IConfiguration configuration, ILogger logger, ITweetProperties tweetProperties)
        {
            _configuration = configuration;
            _logger = logger;
            _properties = tweetProperties;
        }

        public InstrumentData GetInstrumentData(int instrument)
        {
            AccessInstrumentData accessInstrumentData = new(_configuration, _logger);
            var x = accessInstrumentData.GetPrice(instrument);
            return x;
        }

        public WeekData GetWeedEndInstrumentData(int instrument)
        {
            AccessWeekInstrumentData accessWeekEndInstrumentData = new(_configuration, _logger);
            var x = accessWeekEndInstrumentData.GetWeekData(instrument);
            return x;
        }

        public List<Earning> GetEAInstrumentData(int instrument)
        {
            AccessEarningsData accessEarningsData = new(_configuration, _logger);
            var x = accessEarningsData.GetEarningList(instrument.ToString());
            return x;
        }
    }
}