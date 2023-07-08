using MarketPulse.Infrastructure;
using MarketPulse.Models;

namespace MarketPulse.RepositoryModels
{
    internal class PressReleasesRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ITweetProperties _properties;

        public PressReleasesRepository(IConfiguration configuration, ILogger logger, ITweetProperties tweetProperties)
        {
            _configuration = configuration;
            _logger = logger;
            _properties = tweetProperties;
        }

        public List<PressRelease> GetPressReleases(int instrument, string prLanguages, string prSourceIDs)
        {
            AccessPressReleaseData accessPressRelease = new(_configuration, _logger);
            var x = accessPressRelease.GetPressReleaseList(instrument, string.Join(",", prLanguages),
                string.Join(",", prSourceIDs));
            return x;
        }
    }
}