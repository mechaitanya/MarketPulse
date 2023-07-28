using MarketPulse.DbContext;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MarketPulse.Services
{
    public class Credentials
    {
        public string? ConsumerKey { get; set; }
        public string? ConsumerSecret { get; set; }
        public string? AccessToken { get; set; }
        public string? AccessTokenSecret { get; set; }
    }

    public class UserTokens
    {
        private readonly IRSSFeedServiceDbContextFactory _dbContextFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMyLogger _logger;

        public UserTokens(IRSSFeedServiceDbContextFactory dbContextFactory, IServiceProvider serviceProvider, IMyLogger logger)
        {
            _dbContextFactory = dbContextFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public (string? AccessCode, string? AccessSecretToken, string? UserName) GetAccessCodeAndSecretToken(int instrumentId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    using var dbContext = _dbContextFactory.CreateDbContext();
                    var user = dbContext.Users
                        .Where(u => u.InstrumentId == instrumentId)
                        .Select(u => new { u.AccessCode, u.AccessSecretToken, u.UserName })
                        .FirstOrDefault();

                    if (user != null)
                    {
                        return (user.AccessCode, user.AccessSecretToken, user.UserName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"error: {ex.Message} at GetAccessCodeAndSecretToken at {DateTime.UtcNow} UTC");
                }
                return (null, null, null);
            }
        }
    }

    public class TweetSender
    {
        private readonly IConfiguration _configuration;
        private readonly IRSSFeedServiceDbContextFactory _dbContextFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEmailSender _emailSender;
        private readonly IMyLogger _logger;
        private string? Username;
        private const string URL = "https://api.twitter.com/2/tweets";

        public TweetSender(IConfiguration configuration, IRSSFeedServiceDbContextFactory dbContextFactory, IServiceProvider serviceProvider, IEmailSender emailSender, IMyLogger logger)
        {
            _configuration = configuration;
            _dbContextFactory = dbContextFactory;
            _serviceProvider = serviceProvider;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task SendTweet(int instrumentId, string message)
        {
            Credentials creds = new();
            try
            {
                UserTokens ut = new(_dbContextFactory, _serviceProvider, _logger);

                (string? AccessCode, string? AccessSecretToken, string? UserName) = ut.GetAccessCodeAndSecretToken(instrumentId);

                if (AccessCode != null && AccessSecretToken != null)
                {
                    creds.ConsumerKey = _configuration["consumerKey"]!;
                    creds.ConsumerSecret = _configuration["consumerSecret"]!;
                    creds.AccessToken = AccessCode;
                    creds.AccessTokenSecret = AccessSecretToken;
                    Username = UserName ?? "Ghost user";
                }
                else
                {
                    _logger.LogError($"User not found or AccessCode/AccessSecretToken are null at {DateTime.UtcNow} UTC");
                }

                var response = await AuthenticateUserAndSendTweet(creds, message, _logger, Username);

                if (response?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogError($"{Username} is not authorized at {DateTime.UtcNow} UTC");
                    _emailSender.SendAuthorizationFailedEmail(Username);
                }
                if (response?.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogError($"Tweet not sent for {Username} and status code is {response?.StatusCode} or trying to send a duplicate tweet");
                }
                if (response?.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError($"Tweet not sent for {Username} and status code is {response?.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"error: {ex.Message} at SendTweet at {DateTime.UtcNow} UTC");
            }
        }

        public static async Task<HttpResponseMessage?> AuthenticateUserAndSendTweet(Credentials creds, string message, IMyLogger _logger, string Username)
        {
            try
            {
                var httpClient = new HttpClient();
                string json = JsonConvert.SerializeObject(new { text = message });

                var tweetContent = new StringContent(json, Encoding.UTF8, "application/json");
                var oauthNonce = Guid.NewGuid().ToString("N");
                var oauthTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                var oauthSignature = GenerateOAuthSignature(creds.ConsumerKey, creds.ConsumerSecret, creds.AccessToken, creds.AccessTokenSecret, oauthNonce, oauthTimestamp);

                var authorizationHeader = GenerateAuthorizationHeader(creds.ConsumerKey, creds.AccessToken, oauthNonce, oauthTimestamp, oauthSignature);

                httpClient.DefaultRequestHeaders.Add("Authorization", authorizationHeader);

                var response = await httpClient.PostAsync(URL, tweetContent);

                return response;
            }
            catch (Exception)
            {
                _logger.LogError($"{Username} is not authorized at {DateTime.UtcNow} UTC");
                return null;
            }
        }

        private static string GenerateOAuthSignature(string apiKey, string apiSecretKey, string accessToken, string accessTokenSecret, string oauthNonce, string oauthTimestamp)
        {
            var signatureBaseString = string.Format(
                "POST&{0}&{1}",
                Uri.EscapeDataString(URL),
                Uri.EscapeDataString(
                    $"oauth_consumer_key={apiKey}&" +
                    $"oauth_nonce={oauthNonce}&" +
                    $"oauth_signature_method=HMAC-SHA1&" +
                    $"oauth_timestamp={oauthTimestamp}&" +
                    $"oauth_token={accessToken}&" +
                    "oauth_version=1.0"
                )
            );

            var signingKey = $"{Uri.EscapeDataString(apiSecretKey)}&{Uri.EscapeDataString(accessTokenSecret)}";
            var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(signingKey));

            var oauthSignatureBytes = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(signatureBaseString));

            return Convert.ToBase64String(oauthSignatureBytes);
        }

        private static string GenerateAuthorizationHeader(string apiKey, string accessToken, string oauthNonce, string oauthTimestamp, string oauthSignature)
        {
            return $"OAuth " +
                $"oauth_consumer_key=\"{Uri.EscapeDataString(apiKey)}\", " +
                $"oauth_nonce=\"{Uri.EscapeDataString(oauthNonce)}\", " +
                $"oauth_signature=\"{Uri.EscapeDataString(oauthSignature)}\", " +
                $"oauth_signature_method=\"HMAC-SHA1\", " +
                $"oauth_timestamp=\"{Uri.EscapeDataString(oauthTimestamp)}\", " +
                $"oauth_token=\"{Uri.EscapeDataString(accessToken)}\", " +
                "oauth_version=\"1.0\"";
        }
    }
}