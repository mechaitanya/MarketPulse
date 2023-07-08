using MarketPulse.Infrastructure;
using MarketPulse.RepositoryModels;
using static MarketPulse.Infrastructure.AccessInstrumentData;

namespace MarketPulse.Models
{
    internal class TweetBuilder
    {
        private readonly ITweetProperties _properties;
        private readonly ILogger _logger;
        private readonly PressReleasesRepository _pressReleasesRepository;
        private readonly InstrumentDataRepository _instrumentDataRepository;
        private readonly IConfiguration _configuration;

        public TweetBuilder(ITweetProperties tweetProperties, ILogger logger, IConfiguration configuration)
        {
            _properties = tweetProperties;
            _logger = logger;
            _configuration = configuration;
            _pressReleasesRepository = new PressReleasesRepository(_configuration, _logger, _properties);
            _instrumentDataRepository = new InstrumentDataRepository(_configuration, _logger, _properties);
        }

        public List<PressRelease> GetPRATweetMessage()
        {
            if (_properties.TweetType.ToLower() == "pra")
            {
                List<PressRelease> pressReleases = _pressReleasesRepository.GetPressReleases(_properties.InstrumentId, _properties.LanguageID, _properties.SourceID);
                return pressReleases;
            }
            else
            {
                return new List<PressRelease>();
            }
        }

        public InstrumentData GetMOATweetMessage()
        {
            if (_properties.TweetType.ToLower() == "moa")
            {
                InstrumentData iData = _instrumentDataRepository.GetInstrumentData(_properties.InstrumentId);
                return iData;
            }
            else
            {
                return new InstrumentData();
            }
        }

        public InstrumentData GetEODTweetMessage()
        {
            if (_properties.TweetType.ToLower() == "eod")
            {
                InstrumentData iData = _instrumentDataRepository.GetInstrumentData(_properties.InstrumentId);
                return iData;
            }
            else
            {
                return new InstrumentData();
            }
        }

        public WeekData GetEOWTweetMessage()
        {
            if (_properties.TweetType.ToLower() == "eow")
            {
                WeekData iData = _instrumentDataRepository.GetWeedEndInstrumentData(_properties.InstrumentId);
                return iData;
            }
            else
            {
                return new WeekData();
            }
        }

        public List<Earning> GetEATweetMessage()
        {
            if (_properties.TweetType.ToLower() == "ea")
            {
                List<Earning> iData = _instrumentDataRepository.GetEAInstrumentData(_properties.InstrumentId);
                return iData;
            }
            else
            {
                return new List<Earning>();
            }
        }
    }
}