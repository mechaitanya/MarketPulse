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
            try
            {
                AccessInstrumentData accessInstrumentData = new(_configuration, _logger);
                return await accessInstrumentData.GetPrice(instrument);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex.Message} at {DateTime.UtcNow} in GetInstrumentData for instrument: {instrument}");
                return new InstrumentData { };
            }
        }

        public async Task<WeekData> GetWeekEndInstrumentData(int instrument)
        {
            try
            {
                AccessWeekInstrumentData accessWeekEndInstrumentData = new(_configuration, _logger);
                return await accessWeekEndInstrumentData.GetWeekData(instrument);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex.Message} at {DateTime.UtcNow} in GetWeekEndInstrumentData for instrument: {instrument}");
                return new WeekData { };
            }
        }

        public async Task<List<Earning>> GetEAInstrumentData(int instrument)
        {
            try
            {
                AccessEarningsData accessEarningsData = new(_configuration, _logger);
                return await accessEarningsData.GetEarningList(instrument.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex.Message} at {DateTime.UtcNow} in GetEAInstrumentData for instrument: {instrument}");
                return new List<Earning> { };
            }
        }
    }
}