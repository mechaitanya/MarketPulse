using MarketPulse.DbContext;
using Microsoft.EntityFrameworkCore;

namespace MarketPulse.Services
{
    public class TweetService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRSSFeedServiceDbContextFactory _dbContextFactory;

        public TweetService(IServiceProvider serviceProvider, IRSSFeedServiceDbContextFactory dbContextFactory)
        {
            _serviceProvider = serviceProvider;
            _dbContextFactory = dbContextFactory;
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
                var test = dbContext.TweetSchedule.ToList();
                var test1 = dbContext.InstrumentTweets.ToList();
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
                        (schedule, tweet) => new { schedule.InstrumentId, tweet.TweetType }
                    )
                    .Select(t => new { t.InstrumentId, t.TweetType });

                var tweetExecutionTasks = new List<Task>();

                foreach (var tweet in tweets)
                {
                    Task tweetExecutionTask;
                    switch (tweet.TweetType.ToLower())
                    {
                        case "eod":
                            tweetExecutionTask = ExecuteEODTweetAsync(tweet.InstrumentId);
                            break;

                        case "eow":
                            tweetExecutionTask = ExecuteEOWTweetAsync(tweet.InstrumentId);
                            break;

                        case "moa":
                            tweetExecutionTask = ExecuteMOATweetAsync(tweet.InstrumentId);
                            break;

                        case "pra":
                            tweetExecutionTask = ExecutePRATweetAsync(tweet.InstrumentId);
                            break;

                        default:
                            continue;
                    }
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

        private async Task ExecuteEODTweetAsync(int instrumentId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                var eodTemplate = await dbContext.InstrumentTweets
                    .Where(it => it.InstrumentId == instrumentId && it.TweetType == "EOD")
                    .Join(dbContext.TweetTemplates,
                        instrumentTweet => instrumentTweet.TemplateId,
                        template => template.TemplateId,
                        (instrumentTweet, template) => template)
                    .FirstOrDefaultAsync();

                if (eodTemplate != null)
                {
                    Console.WriteLine($"EOD template for instrument ID {instrumentId}: {eodTemplate.MessageText} at {DateTime.Now}");
                }
                else
                {
                    Console.WriteLine($"EOD template not found for instrument ID: {instrumentId}");
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private async Task ExecuteEOWTweetAsync(int instrumentId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                var eowTemplate = await dbContext.InstrumentTweets
                    .Where(it => it.InstrumentId == instrumentId && it.TweetType == "EOW")
                    .Join(dbContext.TweetTemplates,
                        instrumentTweet => instrumentTweet.TemplateId,
                        template => template.TemplateId,
                        (instrumentTweet, template) => template)
                    .FirstOrDefaultAsync();

                if (eowTemplate != null)
                {
                    Console.WriteLine($"EOW template for instrument ID {instrumentId}: {eowTemplate.MessageText} at {DateTime.Now}");
                }
                else
                {
                    Console.WriteLine($"EOW template not found for instrument ID: {instrumentId}");
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private async Task ExecuteMOATweetAsync(int instrumentId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                var data1 = await dbContext.InstrumentTweets.ToListAsync();
                var data = await dbContext.TweetTemplates.ToListAsync();

                var moaTemplate = await dbContext.InstrumentTweets
                    .Where(it => it.InstrumentId == instrumentId && it.TweetType == "MOA")
                    .Join(dbContext.TweetTemplates,
                        instrumentTweet => instrumentTweet.TemplateId,
                        template => template.TemplateId,
                        (instrumentTweet, template) => template)
                    .FirstOrDefaultAsync();

                if (moaTemplate != null)
                {
                    Console.WriteLine($"MOA template for instrument ID {instrumentId}: {moaTemplate.MessageText} at {DateTime.Now}");
                }
                else
                {
                    Console.WriteLine($"MOA template not found for instrument ID: {instrumentId}");
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private async Task ExecutePRATweetAsync(int instrumentId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                var praTemplate = await dbContext.InstrumentTweets
                    .Where(it => it.InstrumentId == instrumentId && it.TweetType == "PRA")
                    .Join(dbContext.TweetTemplates,
                        instrumentTweet => instrumentTweet.TemplateId,
                        template => template.TemplateId,
                        (instrumentTweet, template) => template)
                    .FirstOrDefaultAsync();

                if (praTemplate != null)
                {
                    Console.WriteLine($"PRA template for instrument ID {instrumentId}: {praTemplate.MessageText} at {DateTime.Now}");
                }
                else
                {
                    Console.WriteLine($"PRA template not found for instrument ID: {instrumentId}");
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}