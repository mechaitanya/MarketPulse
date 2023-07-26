namespace MarketPulse.Models
{
    public class Users
    {
        public int InstrumentId { get; set; }
        public string? UserName { get; set; }
        public string? AccessCode { get; set; }
        public string? AccessSecretToken { get; set; }
        public string? CultureCode { get; set; }
        public string? CompanyName { get; set; }
    }

    public class TweetSchedule
    {
        public int ScheduleId { get; set; }
        public int InstrumentId { get; set; }
        public string? TweetDays { get; set; }
        public TimeSpan TweetTime { get; set; }
        public string? TweetFrequencyType { get; set; }
        public int TweetFrequencyValue { get; set; }
        public int TemplateId { get; set; }
        public bool isActive { get; set; }

        public Users User { get; set; }
    }

    public class TweetTemplates
    {
        public int TemplateId { get; set; }
        public string? TweetType { get; set; }
        public string? MessageText { get; set; }
        public string? TweetLink { get; set; }
        public string? SourceId { get; set; }
        public string? LanguageType { get; set; }
        public string? HtmlTemplate { get; set; }
    }

    public class InstrumentTweets
    {
        public int InstrumentTweetId { get; set; }
        public int InstrumentId { get; set; }
        public string? TweetType { get; set; }
        public int TemplateId { get; set; }
        public int ScheduleId { get; set; }

        public Users User { get; set; }
        public TweetTemplates Template { get; set; }
    }

    public class TweetProperties : ITweetProperties
    {
        public int InstrumentId { get; set; }
        public string? TweetType { get; set; }
        public string? TemplateText { get; set; }
        public string? SourceID { get; set; }
        public string? LanguageID { get; set; }
    }

    public interface ITweetProperties
    {
        int InstrumentId { get; set; }
        string TweetType { get; set; }
        string TemplateText { get; set; }
        string SourceID { get; set; }
        string LanguageID { get; set; }
    }
}