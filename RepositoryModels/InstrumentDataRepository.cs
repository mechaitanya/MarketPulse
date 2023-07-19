using MarketPulse.Infrastructure;
using MarketPulse.Models;
using static MarketPulse.Infrastructure.AccessInstrumentData;

namespace MarketPulse.RepositoryModels
{
    internal class InstrumentDataRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IMyLogger _logger;
        private readonly ITweetProperties _properties;

        public InstrumentDataRepository(IConfiguration configuration, IMyLogger logger, ITweetProperties tweetProperties)
        {
            _configuration = configuration;
            _logger = logger;
            _properties = tweetProperties;
        }

        public async Task<InstrumentData> GetInstrumentData(int instrument)
        {
            AccessInstrumentData accessInstrumentData = new(_configuration, _logger);
            var x = await accessInstrumentData.GetPrice(instrument);
            return x;
        }

        public async Task<WeekData> GetWeedEndInstrumentData(int instrument)
        {
            AccessWeekInstrumentData accessWeekEndInstrumentData = new(_configuration, _logger);
            var x = await accessWeekEndInstrumentData.GetWeekData(instrument);
            return x;
        }

        public async Task<List<Earning>> GetEAInstrumentData(int instrument)
        {
            AccessEarningsData accessEarningsData = new(_configuration, _logger);
            var x = await accessEarningsData.GetEarningList(instrument.ToString());
            return x;
        }
    }
}