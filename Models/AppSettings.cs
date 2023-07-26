namespace MarketPulse.Models
{
    public record AppSettings(string? SupportEmail, string? SmtpHost, string? ConsumerKey, string? ConsumerSecret, string? ServerFilePath);

    public class AppConfig
    {
        private readonly IConfiguration _configuration;
        private readonly IMyLogger _myLogger;

        public AppConfig(IConfiguration configuration, IMyLogger myLogger)
        {
            _configuration = configuration;
            _myLogger = myLogger;
        }

        public void GetConfig()
        {
            try
            {
                AppSettings? settings = _configuration.Get<AppSettings>();
                if (settings != null)
                {
                    string? supportEmail = settings.SupportEmail;
                    string? smtpHost = settings.SmtpHost;
                    string? consumerKey = settings.ConsumerKey;
                    string? consumerSecret = settings.ConsumerSecret;
                    string? serverFilePath = settings.ServerFilePath;
                }
                else
                {
                    _myLogger.LogWarning($"Configuration settings are not available at {DateTime.UtcNow} UTC");
                }
            }
            catch (Exception ex)
            {
                _myLogger.LogWarning($"Error reading configurations: {ex.Message} at {DateTime.UtcNow} UTC");
            }
        }
    }
}