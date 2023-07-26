using MarketPulse.Infrastructure;
using MarketPulse.Models;

namespace MarketPulse.RepositoryModels
{
    internal class PressReleasesRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IMyLogger _logger;
        private readonly ITweetProperties _properties;

        public PressReleasesRepository(IConfiguration configuration, IMyLogger logger, ITweetProperties tweetProperties)
        {
            _configuration = configuration;
            _logger = logger;
            _properties = tweetProperties;
        }

        public List<PressRelease> GetPressReleases(int instrument, string prLanguages, string prSourceIDs)
        {
            try
            {
                AccessPressReleaseData accessPressRelease = new(_configuration, _logger);
                return accessPressRelease.GetPressReleaseList(instrument, string.Join(",", prLanguages),
                    string.Join(",", prSourceIDs));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex.Message} in GetPressReleases for instrument: {instrument}");
                return new List<PressRelease>();
            }
        }
    }
}