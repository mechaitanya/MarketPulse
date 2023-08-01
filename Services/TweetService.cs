using MarketPulse.DbContext;
using MarketPulse.Infrastructure;
using MarketPulse.Models;
using MarketPulse.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace MarketPulse.Services
{
    public class TweetService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRSSFeedServiceDbContextFactory _dbContextFactory;
        private readonly IMyLogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly IEmailSender _emailSender;

        public TweetService(IServiceProvider serviceProvider, IRSSFeedServiceDbContextFactory dbContextFactory,
            IMyLogger logger, IConfiguration configuration, IMemoryCache memoryCache, IEmailSender emailSender)
        {
            _serviceProvider = serviceProvider;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _emailSender = emailSender;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(ExecuteScheduledTweets, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private async void ExecuteScheduledTweets(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                AccessTimeZoneData accessTimeZoneData = new(_configuration, _logger);
                var currentTime = DateTime.UtcNow.TimeOfDay;
                var currentDayOfWeek = DateTime.UtcNow.DayOfWeek.ToString();

                var tweets = dbContext.TweetSchedule
                    .AsNoTracking()
                    .ToList()
                    .Where(t =>
                        t.TweetDays.Contains(currentDayOfWeek) &&
                        t.TweetTime <= currentTime &&
                        t.isActive == true &&
                        IsTweetTimeMatchingFrequency(t.TweetTime, t.TweetFrequencyType, t.TweetFrequencyValue))
                    .Join(
                        dbContext.InstrumentTweets,
                        schedule => new { schedule.InstrumentId, schedule.TemplateId },
                        tweet => new { tweet.InstrumentId, tweet.TemplateId },
                        (schedule, tweet) => new { schedule.InstrumentId, tweet.TweetType, schedule.TweetTime, tweet.ScheduleId, schedule.TemplateId })
                    .ToList()
                    .Select(tweet => {
                        var date = accessTimeZoneData.GetDayLightSavingTime(tweet.InstrumentId, DateTime.UtcNow.Add(tweet.TweetTime));
                        return new
                        {
                            tweet.InstrumentId,
                            tweet.TweetType,
                            TweetTime = date, 
                            tweet.ScheduleId,
                            tweet.TemplateId
                        };
                    }).ToList();

                var tweetExecutionTasks = new List<Task>();

                foreach (var tweet in tweets)
                {
                    Task tweetExecutionTask;
                    DateTime localDateTime = TimeZoneInfo.ConvertTimeFromUtc(tweet.TweetTime, TimeZoneInfo.Local);

                    tweetExecutionTask = ExecuteTweetAsync(tweet.InstrumentId, tweet.TweetType, localDateTime);
                    tweetExecutionTasks.Add(tweetExecutionTask);
                }

                while (tweetExecutionTasks.Any())
                {
                    Task completedTask = await Task.WhenAny(tweetExecutionTasks);
                    tweetExecutionTasks.Remove(completedTask);
                }
            }
        }

        private static bool IsTweetTimeMatchingFrequency(TimeSpan tweetTime, string frequencyType, int frequencyValue)
        {
            var currentTime = DateTime.UtcNow.TimeOfDay;

            switch (frequencyType.ToLower())
            {
                case "minutes":
                    var minutesDifference = currentTime.TotalMinutes - tweetTime.TotalMinutes;
                    return minutesDifference >= 0 && (int)minutesDifference % frequencyValue == 0;

                case "hourly":
                    var hourDifference = (currentTime - tweetTime).TotalHours;
                    return hourDifference >= 0 && (int)hourDifference % frequencyValue == 0;

                case "daily":
                    var interval = TimeSpan.FromMinutes(24 * 60 / frequencyValue);
                    return currentTime - tweetTime >= TimeSpan.Zero && (currentTime - tweetTime).TotalMinutes % interval.TotalMinutes < 1;

                case "weekly":
                    var daysSinceEpoch = (DateTime.Now - new DateTime(1970, 1, 1)).TotalDays;
                    var weekDifference = (daysSinceEpoch - tweetTime.TotalDays) / 7;
                    return weekDifference >= 0 && weekDifference % frequencyValue == 0;

                case "monthly":
                    var monthDifference = (currentTime - tweetTime).TotalDays / 30;
                    return monthDifference >= 0 && monthDifference % frequencyValue == 0;

                default:
                    return false;
            }
        }

        private async Task ExecuteTweetAsync(int instrumentId, string tweetType, DateTime dateTime)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                var TweetTemplate = await dbContext.InstrumentTweets
                    .Where(it => it.InstrumentId == instrumentId && it.TweetType == tweetType)
                    .Join(dbContext.TweetTemplates,
                        instrumentTweet => instrumentTweet.TemplateId,
                        template => template.TemplateId,
                        (instrumentTweet, template) => template)
                    .FirstOrDefaultAsync();

                if (TweetTemplate != null)
                {
                    if (tweetType.ToLower() == "pra")
                    {
                        await ProcessPRATweets(instrumentId, TweetTemplate, tweetType.ToLower(), dateTime);
                    }
                    else if (tweetType.ToLower() == "moa" || tweetType.ToLower() == "mca")
                    {
                        await ProcessMOATweets(instrumentId, TweetTemplate, tweetType.ToLower(), dateTime);
                    }
                    else if (tweetType.ToLower() == "eod")
                    {
                        await ProcessEODTweets(instrumentId, TweetTemplate, tweetType.ToLower(), dateTime);
                    }
                    else if (tweetType.ToLower() == "eow")
                    {
                        await ProcessEOWTweets(instrumentId, TweetTemplate, tweetType.ToLower(), dateTime);
                    }
                    else if (tweetType.ToLower() == "ea")
                    {
                        await ProcessEATweets(instrumentId, TweetTemplate, tweetType.ToLower(), dateTime);
                    }
                    else
                    {
                        Console.WriteLine($"{tweetType} template for instrument ID {instrumentId}: {TweetTemplate.MessageText} at {DateTime.Now}");
                    }
                }
                else
                {
                    Console.WriteLine($"{tweetType} template not found for instrument ID: {instrumentId}");
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private async Task ProcessPRATweets(int instrumentId, TweetTemplates tweetTemplate, string tweetType, DateTime dTime)
        {
            TweetProperties _tweetProperties = new()
            {
                InstrumentId = instrumentId,
                TweetType = tweetType,
                TemplateText = tweetTemplate.MessageText,
                SourceID = tweetTemplate.SourceId,
                LanguageID = tweetTemplate.LanguageType
            };

            TweetSender ts = new(_configuration, _dbContextFactory, _serviceProvider, _emailSender, _logger);
            TweetBuilder tweetBuilder = new(_tweetProperties, _logger, _configuration);
            var totaltweets = tweetBuilder.GetPRATweetMessage();

            if (totaltweets?.Count > 0)
            {
                foreach (var PRtweet in totaltweets)
                {
                    if (!IsPublicHoliday(PRtweet.PR_Instrument_ID, dTime))
                    {
                        var text = MakeTweet(PRtweet, tweetTemplate.MessageText);
                        //await ts.SendTweet(instrumentId, text);
                        Console.WriteLine($"{text} at {DateTime.Now} and Instrument ID: {PRtweet.PR_Instrument_ID}");
                    }
                }
            }
            await Task.Delay(5);
        }

        private async Task ProcessMOATweets(int instrumentId, TweetTemplates tweetTemplate, string tweetType, DateTime dTime)
        {
            TweetProperties _tweetProperties = new()
            {
                InstrumentId = instrumentId,
                TweetType = tweetType,
                TemplateText = tweetTemplate.MessageText,
                SourceID = tweetTemplate.SourceId,
                LanguageID = tweetTemplate.LanguageType
            };

            TweetSender ts = new(_configuration, _dbContextFactory, _serviceProvider, _emailSender, _logger);

            TweetBuilder tweetBuilder = new(_tweetProperties, _logger, _configuration);
            var instrumentData = await tweetBuilder.GetMOATweetMessage();

            var fileName = tweetType + "-" + DateTime.Now.ToString("yyMMdd");
            if (tweetTemplate.TweetLink != null && tweetTemplate.TweetLink.Contains("filename"))
            {
                tweetTemplate.TweetLink = tweetTemplate.TweetLink.Replace("{filename}", fileName);
            }

            if (!IsPublicHoliday(instrumentId, dTime))
            {
                if (tweetTemplate.TweetLink != null && tweetTemplate.HtmlTemplate != null)
                {
                    CreateImage cImage = new(_configuration, _logger);
                    cImage.CreateInteractiveImageWithGraph(instrumentId, MakeTweet(instrumentData, tweetTemplate.HtmlTemplate), fileName,
                       Path.GetExtension(tweetTemplate.TweetLink).ToLower(), instrumentData.Ticker ?? "test");
                    var text = MakeTweet(instrumentData, tweetTemplate.MessageText + " " + tweetTemplate.TweetLink ?? " ".Trim());
                    //await ts.SendTweet(instrumentId, text);
                    Console.WriteLine($"{text} at {DateTime.Now} and Instrument ID: {instrumentId}");
                }
                else
                {
                    var text = MakeTweet(instrumentData, tweetTemplate.MessageText);
                    //await ts.SendTweet(instrumentId, text);
                    Console.WriteLine($"{text} at {DateTime.Now} and Instrument ID: {instrumentId}");
                }
            }

            await Task.Delay(5);
        }

        private async Task ProcessEODTweets(int instrumentId, TweetTemplates tweetTemplate, string tweetType, DateTime dTime)
        {
            TweetProperties _tweetProperties = new()
            {
                InstrumentId = instrumentId,
                TweetType = tweetType,
                TemplateText = tweetTemplate.MessageText,
                SourceID = tweetTemplate.SourceId,
                LanguageID = tweetTemplate.LanguageType
            };

            TweetSender ts = new(_configuration, _dbContextFactory, _serviceProvider, _emailSender, _logger);

            TweetBuilder tweetBuilder = new(_tweetProperties, _logger, _configuration);
            var instrumentData = await tweetBuilder.GetEODTweetMessage();

            var fileName = tweetType + "-" + DateTime.UtcNow.ToString("yyMMdd");
            if (tweetTemplate.TweetLink != null && tweetTemplate.TweetLink.Contains("filename"))
            {
                tweetTemplate.TweetLink = tweetTemplate.TweetLink.Replace("{filename}", fileName);
            }

            if (!IsPublicHoliday(instrumentId, dTime))
            {
                if (tweetTemplate.TweetLink != null && tweetTemplate.HtmlTemplate != null)
                {
                    CreateImage cImage = new(_configuration, _logger);
                    cImage.CreateInteractiveImageWithGraph(instrumentId, MakeTweet(instrumentData, tweetTemplate.HtmlTemplate), fileName,
                        Path.GetExtension(tweetTemplate.TweetLink).ToLower(), string.IsNullOrEmpty(instrumentData.Ticker) ? instrumentData.Ticker : "test");
                    var text = MakeTweet(instrumentData, tweetTemplate.MessageText + " " + tweetTemplate.TweetLink ?? " ".Trim());
                    //await ts.SendTweet(instrumentId, text);
                    Console.WriteLine($"{text} at {DateTime.Now} and Instrument ID: {instrumentId}");
                }
                else
                {
                    var text = MakeTweet(instrumentData, tweetTemplate.MessageText);
                    //await ts.SendTweet(instrumentId, text);
                    Console.WriteLine($"{text} at {DateTime.Now}");
                }
            }
            await Task.Delay(5);
        }

        private async Task ProcessEOWTweets(int instrumentId, TweetTemplates tweetTemplate, string tweetType, DateTime dTime)
        {
            var _tweetProperties = new TweetProperties
            {
                InstrumentId = instrumentId,
                TweetType = tweetType,
                TemplateText = tweetTemplate.MessageText,
                SourceID = tweetTemplate.SourceId,
                LanguageID = tweetTemplate.LanguageType
            };

            TweetSender ts = new(_configuration, _dbContextFactory, _serviceProvider, _emailSender, _logger);

            var tweetBuilder = new TweetBuilder(_tweetProperties, _logger, _configuration);
            var instrumentData = await tweetBuilder.GetEODTweetMessage();
            var weekData = await tweetBuilder.GetEOWTweetMessage();

            var fileName = tweetType + "-" + DateTime.UtcNow.ToString("yyMMdd");
            if (tweetTemplate.TweetLink != null && tweetTemplate.TweetLink.Contains("filename"))
            {
                tweetTemplate.TweetLink = tweetTemplate.TweetLink.Replace("{filename}", fileName);
            }

            if (!IsPublicHoliday(instrumentId, dTime))
            {
                if (tweetTemplate.TweetLink != null && tweetTemplate.HtmlTemplate != null)
                {
                    var htmlTemplate = MakeTweet(weekData, tweetTemplate.HtmlTemplate);
                    htmlTemplate = MakeTweet(instrumentData, htmlTemplate);
                    var cImage = new CreateImage(_configuration, _logger);
                    cImage.CreateInteractiveImageWithGraph(Convert.ToInt32(instrumentId), htmlTemplate, fileName,
                        Path.GetExtension(tweetTemplate.TweetLink).ToLower(), instrumentData.Ticker ?? "test");
                    var text = MakeTweet(weekData, $"{tweetTemplate.MessageText} {(tweetTemplate.TweetLink != null ? tweetTemplate.TweetLink.Replace("{ticker}", instrumentData.Ticker ?? "test").Trim() : " ")}");
                    //await ts.SendTweet(instrumentId, text);
                    Console.WriteLine($"{text} at {DateTime.Now} and Instrument ID: {instrumentId}");
                }
                else
                {
                    var text = MakeTweet(weekData, tweetTemplate.MessageText);
                    //await ts.SendTweet(instrumentId, text);
                    Console.WriteLine($"{text} at {DateTime.Now}");
                }
            }

            await Task.Delay(5);
        }

        private async Task ProcessEATweets(int instrumentId, TweetTemplates tweetTemplate, string tweetType, DateTime dTime)
        {
            TweetProperties _tweetProperties = new()
            {
                InstrumentId = instrumentId,
                TweetType = tweetType,
                TemplateText = tweetTemplate.MessageText,
                SourceID = tweetTemplate.SourceId,
                LanguageID = tweetTemplate.LanguageType
            };

            TweetSender ts = new(_configuration, _dbContextFactory, _serviceProvider, _emailSender, _logger);

            TweetBuilder tweetBuilder = new(_tweetProperties, _logger, _configuration);
            var earningData = await tweetBuilder.GetEATweetMessage();

            foreach (var earning in earningData)
            {
                if (!IsPublicHoliday(instrumentId, dTime))
                {
                    var text = MakeTweet(earning, tweetTemplate.MessageText);
                    //await ts.SendTweet(instrumentId, text);
                    Console.WriteLine($"{text} at {DateTime.Now}");
                }
            }
            await Task.Delay(5);
        }

        public static string MakeTweet<T>(T data, string messageText)
        {
            try
            {
                string pattern = @"\{([^{}]+)\}:\{([^{}]+)\}";
                MatchCollection matches = Regex.Matches(messageText, pattern);

                Dictionary<string, string> keyValuePairs = new();

                foreach (Match match in matches)
                {
                    string placeholder = match.Groups[0].Value;
                    string key = match.Groups[1].Value;
                    string formatSpecifier = match.Groups[2].Value;

                    keyValuePairs.Add(key, formatSpecifier);
                }

                if (matches.Count > 0)
                {
                    if (data != null)
                    {
                        var dataProperties = typeof(T).GetProperties();

                        foreach (var property in dataProperties)
                        {
                            string propertyName = property.Name.ToLower();
                            string placeholder = $"{{{propertyName.ToLower()}}}";
                            string propertyValue = property.GetValue(data)?.ToString();

                            if (messageText.Contains(placeholder))
                            {
                                if (keyValuePairs.TryGetValue(propertyName, out var formatSpecifier))
                                {
                                    propertyValue = DataFormatter.ApplyFormatSpecifier(propertyValue, formatSpecifier);
                                }
                                if (keyValuePairs.ContainsKey(propertyName))
                                {
                                    messageText = messageText.Replace(placeholder, propertyValue).Replace(":{" + keyValuePairs[propertyName] + "}", " ").Trim();
                                }
                                messageText = Regex.Replace(messageText, @"\s+", " ");
                            }
                        }
                    }
                }
                if (data != null)
                {
                    var dataProperties = typeof(T).GetProperties();

                    foreach (var property in dataProperties)
                    {
                        string propertyName = property.Name.ToLower();
                        string placeholder = $"{{{propertyName.ToLower()}}}";
                        string propertyValue = property.GetValue(data)?.ToString();

                        if (messageText.Contains(placeholder))
                        {
                            messageText = messageText.Replace(placeholder, propertyValue).Trim();
                            messageText = Regex.Replace(messageText, @"\s+", " ");
                        }
                    }
                }
                return messageText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex.Message} at {DateTime.Now}");
                return null;
            }
        }

        public bool IsPublicHoliday(long instrumentId, DateTime dateTime)
        {
            AccessPublicHolidayData accessPublicHolidayData = new(_configuration, _logger);
            if (_memoryCache.TryGetValue("HolidaysCacheKey", out List<PublicHoliday> holidays))
            {
                return accessPublicHolidayData.CheckPublicHoliday(instrumentId, dateTime, holidays);
            }
            else
            {
                CacheUpdatingService _CUS = new(_memoryCache, _logger, _configuration, _serviceProvider, _dbContextFactory);
                _CUS.UpdateCache(null);

                if (_memoryCache.TryGetValue("HolidaysCacheKey", out holidays))
                {
                    return accessPublicHolidayData.CheckPublicHoliday(instrumentId, dateTime, holidays);
                }
                else
                {
                    return false;
                }
            }
        }

        public void Dispose() => _timer?.Dispose();
    }
}