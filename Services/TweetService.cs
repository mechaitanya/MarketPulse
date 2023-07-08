﻿using MarketPulse.DbContext;
using MarketPulse.Infrastructure;
using MarketPulse.Models;
using MarketPulse.Utility;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using static MarketPulse.Infrastructure.AccessInstrumentData;

namespace MarketPulse.Services
{
    public class TweetService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRSSFeedServiceDbContextFactory _dbContextFactory;
        private readonly ITweetProperties _tweetProperties;
        private readonly ILogger<TweetService> _logger;
        private readonly IConfiguration _configuration;

        public TweetService(IServiceProvider serviceProvider, IRSSFeedServiceDbContextFactory dbContextFactory, ITweetProperties tweetProperties, ILogger<TweetService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _dbContextFactory = dbContextFactory;
            _tweetProperties = tweetProperties;
            _logger = logger;
            _configuration = configuration;
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

                var currentTime = DateTime.Now.TimeOfDay;
                var currentDayOfWeek = DateTime.Now.DayOfWeek.ToString();
                AccessTimeZoneData DayLight = new(_configuration, _logger);

                var tweets = dbContext.TweetSchedule
                    .AsNoTracking()
                    .ToList()
                    .Where(t =>
                        t.TweetDays.Contains(currentDayOfWeek) &&
                        t.TweetTime <= currentTime &&
                        IsTweetTimeMatchingFrequency(t.TweetTime, t.TweetFrequencyType, t.TweetFrequencyValue))
                    .Join(
                        dbContext.InstrumentTweets,
                        schedule => schedule.InstrumentId,
                        tweet => tweet.InstrumentId,
                        (schedule, tweet) => new { schedule.InstrumentId, tweet.TweetType, schedule.TweetTime });

                var tweetExecutionTasks = new List<Task>();

                foreach (var tweet in tweets)
                {
                    Task tweetExecutionTask;
                    tweetExecutionTask = ExecuteTweetAsync(tweet.InstrumentId, tweet.TweetType, DateTime.Today.Add(tweet.TweetTime));
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
            var currentTime = DateTime.Now.TimeOfDay;

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
            _tweetProperties.InstrumentId = instrumentId;
            _tweetProperties.TweetType = tweetType;
            _tweetProperties.TemplateText = tweetTemplate.MessageText;
            _tweetProperties.SourceID = tweetTemplate.SourceId;
            _tweetProperties.LanguageID = tweetTemplate.LanguageType;
            
            AccessTimeZoneData accessTimeZoneData = new(_configuration, _logger);
            var date = accessTimeZoneData.GetDayLightSavingTime(instrumentId, dTime);

            TweetBuilder tweetBuilder = new(_tweetProperties, _logger, _configuration);
            var totaltweets = tweetBuilder.GetPRATweetMessage();
            

            if (totaltweets?.Count > 0)
            {
                foreach (var PRtweet in totaltweets)
                {
                    if (!IsPublicHoliday(PRtweet.PR_Instrument_ID, date))
                    {
                        Console.WriteLine($"pra template for instrument ID {instrumentId}: {PRtweet.PR_Link} at {DateTime.Now}");
                    }
                }
            }
            await Task.Delay(5);
        }

        private async Task ProcessMOATweets(int instrumentId, TweetTemplates tweetTemplate, string tweetType, DateTime dTime)
        {
            _tweetProperties.InstrumentId = instrumentId;
            _tweetProperties.TweetType = tweetType;
            _tweetProperties.TemplateText = tweetTemplate.MessageText;
            _tweetProperties.SourceID = tweetTemplate.SourceId;
            _tweetProperties.LanguageID = tweetTemplate.LanguageType;

            AccessTimeZoneData accessTimeZoneData = new(_configuration, _logger);
            var date = accessTimeZoneData.GetDayLightSavingTime(instrumentId, dTime);

            TweetBuilder tweetBuilder = new(_tweetProperties, _logger, _configuration);
            var instrumentData = tweetBuilder.GetMOATweetMessage();
          

            if (!IsPublicHoliday(instrumentId, date))
            {
                var text = MakeTweet(instrumentData, tweetTemplate.MessageText);
                Console.WriteLine($"{text} at {DateTime.Now}");
            }

            await Task.Delay(5);
        }

        private async Task ProcessEODTweets(int instrumentId, TweetTemplates tweetTemplate, string tweetType, DateTime dTime)
        {
            _tweetProperties.InstrumentId = instrumentId;
            _tweetProperties.TweetType = tweetType;
            _tweetProperties.TemplateText = tweetTemplate.MessageText;
            _tweetProperties.SourceID = tweetTemplate.SourceId;
            _tweetProperties.LanguageID = tweetTemplate.LanguageType;

            AccessTimeZoneData accessTimeZoneData = new(_configuration, _logger);
            var date = accessTimeZoneData.GetDayLightSavingTime(instrumentId, dTime);

            TweetBuilder tweetBuilder = new(_tweetProperties, _logger, _configuration);
            var instrumentData = tweetBuilder.GetEODTweetMessage();
     
            if (!IsPublicHoliday(instrumentId, date))
            {
                //var text = MakeInstrumentTweet(instrumentData, tweetTemplate.MessageText);
                var text = MakeTweet(instrumentData, tweetTemplate.MessageText);
                Console.WriteLine($"{text} at {DateTime.Now}");
            }
            await Task.Delay(5);
        }

        private async Task ProcessEOWTweets(int instrumentId, TweetTemplates tweetTemplate, string tweetType, DateTime dTime)
        {
            _tweetProperties.InstrumentId = instrumentId;
            _tweetProperties.TweetType = tweetType;
            _tweetProperties.TemplateText = tweetTemplate.MessageText;
            _tweetProperties.SourceID = tweetTemplate.SourceId;
            _tweetProperties.LanguageID = tweetTemplate.LanguageType;

            AccessTimeZoneData accessTimeZoneData = new(_configuration, _logger);
            var date = accessTimeZoneData.GetDayLightSavingTime(instrumentId, dTime);

            TweetBuilder tweetBuilder = new(_tweetProperties, _logger, _configuration);
            var weekData = tweetBuilder.GetEOWTweetMessage();

            if (!IsPublicHoliday(instrumentId, date))
            {
                var text = MakeTweet(weekData, tweetTemplate.MessageText);
                Console.WriteLine($"{text} at {DateTime.Now}");
            }
            await Task.Delay(5);
        }

        private async Task ProcessEATweets(int instrumentId, TweetTemplates tweetTemplate, string tweetType, DateTime dTime)
        {
            _tweetProperties.InstrumentId = instrumentId;
            _tweetProperties.TweetType = tweetType;
            _tweetProperties.TemplateText = tweetTemplate.MessageText;
            _tweetProperties.SourceID = tweetTemplate.SourceId;
            _tweetProperties.LanguageID = tweetTemplate.LanguageType;

            AccessTimeZoneData accessTimeZoneData = new(_configuration, _logger);
            var date = accessTimeZoneData.GetDayLightSavingTime(instrumentId, dTime);

            TweetBuilder tweetBuilder = new(_tweetProperties, _logger, _configuration);
            var earningData = tweetBuilder.GetEATweetMessage();

            foreach (var earning in earningData)
            {
                if (!IsPublicHoliday(instrumentId, date))
                {
                    var text = MakeTweet(earning, tweetTemplate.MessageText);
                    Console.WriteLine($"{text} at {DateTime.Now}");
                }
            }
            await Task.Delay(5);
        }

        public static string MakeTweet<T>(T data, string messageText)
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
                        if (keyValuePairs.TryGetValue(propertyName.ToLower(), out var formatSpecifier))
                        {
                            propertyValue = DataFormatter.ApplyFormatSpecifier(propertyValue, formatSpecifier);
                        }

                        messageText = messageText.Replace(placeholder, propertyValue).Replace(":{" + keyValuePairs[propertyName] + "}", " ").Trim();
                        messageText = Regex.Replace(messageText, @"\s+", " ");
                    }
                }
            }

            return messageText;
        }

        //public static string MakeInstrumentTweet(InstrumentData instrumentData, string MessageText)
        //{
        //    string pattern = @"\{([^{}]+)\}:\{([^{}]+)\}";
        //    MatchCollection matches = Regex.Matches(MessageText, pattern);

        //    Dictionary<string, string> keyValuePairs = new();

        //    foreach (Match match in matches)
        //    {
        //        string placeholder = match.Groups[0].Value;
        //        string key = match.Groups[1].Value;
        //        string formatSpecifier = match.Groups[2].Value;

        //        keyValuePairs.Add(key, formatSpecifier);
        //    }

        //    if (instrumentData != null)
        //    {
        //        var instrumentProperties = typeof(InstrumentData).GetProperties();

        //        foreach (var property in instrumentProperties)
        //        {
        //            string propertyName = property.Name.ToLower();
        //            string placeholder = $"{{{propertyName.ToLower()}}}";
        //            string propertyValue = property.GetValue(instrumentData)?.ToString();

        //            if (MessageText.Contains(placeholder))
        //            {
        //                if (keyValuePairs.TryGetValue(propertyName.ToLower(), out string formatSpecifier))
        //                {
        //                    propertyValue = DataFormatter.ApplyFormatSpecifier(propertyValue, formatSpecifier);
        //                }

        //                MessageText = MessageText.Replace(placeholder, propertyValue).Replace(":{" + keyValuePairs[propertyName] + "}", " ").Trim();
        //                MessageText = Regex.Replace(MessageText, @"\s+", " ");
        //            }
        //        }
        //    }
        //    return MessageText;
        //}

        //public static string MakeWeekEndInstrumentTweet(WeekData weekData, string MessageText)
        //{
        //    string pattern = @"\{([^{}]+)\}:\{([^{}]+)\}";
        //    MatchCollection matches = Regex.Matches(MessageText, pattern);

        //    Dictionary<string, string> keyValuePairs = new();

        //    foreach (Match match in matches)
        //    {
        //        string placeholder = match.Groups[0].Value;
        //        string key = match.Groups[1].Value;
        //        string formatSpecifier = match.Groups[2].Value;

        //        keyValuePairs.Add(key, formatSpecifier);
        //    }

        //    if (weekData != null)
        //    {
        //        var instrumentProperties = typeof(WeekData).GetProperties();

        //        foreach (var property in instrumentProperties)
        //        {
        //            string propertyName = property.Name.ToLower();
        //            string placeholder = $"{{{propertyName.ToLower()}}}";
        //            string propertyValue = property.GetValue(weekData)?.ToString();

        //            if (MessageText.Contains(placeholder))
        //            {
        //                if (keyValuePairs.TryGetValue(propertyName.ToLower(), out var formatSpecifier))
        //                {
        //                    propertyValue = DataFormatter.ApplyFormatSpecifier(propertyValue, formatSpecifier);
        //                }

        //                MessageText = MessageText.Replace(placeholder, propertyValue).Replace(":{" + keyValuePairs[propertyName] + "}", " ").Trim();
        //                MessageText = Regex.Replace(MessageText, @"\s+", " ");
        //            }
        //        }
        //    }
        //    return MessageText;
        //}

        //public static string MakeEATweet(List<Earning> earning, string MessageText)
        //{
        //    string pattern = @"\{([^{}]+)\}:\{([^{}]+)\}";
        //    MatchCollection matches = Regex.Matches(MessageText, pattern);

        //    Dictionary<string, string> keyValuePairs = new();

        //    foreach (Match match in matches)
        //    {
        //        string placeholder = match.Groups[0].Value;
        //        string key = match.Groups[1].Value;
        //        string formatSpecifier = match.Groups[2].Value;

        //        keyValuePairs.Add(key, formatSpecifier);
        //    }

        //    if (earning != null)
        //    {
        //        var instrumentProperties = typeof(WeekData).GetProperties();

        //        foreach (var property in instrumentProperties)
        //        {
        //            string propertyName = property.Name.ToLower();
        //            string placeholder = $"{{{propertyName.ToLower()}}}";
        //            string propertyValue = property.GetValue(earning)?.ToString();

        //            if (MessageText.Contains(placeholder))
        //            {
        //                if (keyValuePairs.TryGetValue(propertyName.ToLower(), out var formatSpecifier))
        //                {
        //                    propertyValue = DataFormatter.ApplyFormatSpecifier(propertyValue, formatSpecifier);
        //                }

        //                MessageText = MessageText.Replace(placeholder, propertyValue).Replace(":{" + keyValuePairs[propertyName] + "}", " ").Trim();
        //                MessageText = Regex.Replace(MessageText, @"\s+", " ");
        //            }
        //        }
        //    }
        //    return MessageText;
        //}

        public bool IsPublicHoliday(long instrumentId, DateTime dateTime)
        {
            AccessPublicHolidayData accessPublicHolidayData = new(_configuration, _logger);
            var listHolidays = accessPublicHolidayData.SelectAllPublicHolidays(instrumentId.ToString());
            return accessPublicHolidayData.CheckPublicHoliday(instrumentId, dateTime, listHolidays);
        }

        public void Dispose() => _timer?.Dispose();
    }
}