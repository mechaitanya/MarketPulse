using MarketPulse.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;

public class RSSFeedServiceDbContext : DbContext
{
    private readonly string _connectionString;

    public RSSFeedServiceDbContext(DbContextOptions<RSSFeedServiceDbContext> options) : base(options)
        => _connectionString = options.GetExtension<SqlServerOptionsExtension>().ConnectionString;

    public DbSet<Users> Users { get; set; }
    public DbSet<TweetSchedule> TweetSchedule { get; set; }
    public DbSet<TweetTemplates> TweetTemplates { get; set; }
    public DbSet<InstrumentTweets> InstrumentTweets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Users>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(e => e.InstrumentId);

            entity.Property(e => e.InstrumentId).HasColumnName("InstrumentId");
            entity.Property(e => e.UserName).HasColumnName("UserName");
            entity.Property(e => e.AccessCode).HasColumnName("AccessCode");
            entity.Property(e => e.AccessSecretToken).HasColumnName("AccessSecretToken");
            entity.Property(e => e.CultureCode).HasColumnName("CultureCode");
            entity.Property(e => e.CompanyName).HasColumnName("CompanyName");
        });

        modelBuilder.Entity<TweetSchedule>(entity =>
        {
            entity.ToTable("TweetSchedule");

            entity.HasKey(e => e.ScheduleId);

            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleId");
            entity.Property(e => e.InstrumentId).HasColumnName("InstrumentId");
            entity.Property(e => e.TweetDays).HasColumnName("TweetDays");
            entity.Property(e => e.TweetTime).HasColumnName("TweetTime");
            entity.Property(e => e.TweetFrequencyType).HasColumnName("TweetFrequencyType");
            entity.Property(e => e.TweetFrequencyValue).HasColumnName("TweetFrequencyValue");
            entity.Property(e => e.TemplateId).HasColumnName("TemplateId");
            entity.Property(e => e.isActive).HasColumnName("isActive");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.InstrumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TweetTemplates>(entity =>
        {
            entity.ToTable("TweetTemplates");

            entity.HasKey(e => e.TemplateId);

            entity.Property(e => e.TemplateId).HasColumnName("TemplateId");
            entity.Property(e => e.TweetType).HasColumnName("TweetType");
            entity.Property(e => e.MessageText).HasColumnName("MessageText").IsRequired(false);
            entity.Property(e => e.TweetLink).HasColumnName("TweetLink").IsRequired(false);
            entity.Property(e => e.SourceId).HasColumnName("SourceId").IsRequired(false);
            entity.Property(e => e.LanguageType).HasColumnName("LanguageType");
            entity.Property(e => e.HtmlTemplate).HasColumnName("HtmlTemplate");
        });

        modelBuilder.Entity<InstrumentTweets>(entity =>
        {
            entity.ToTable("InstrumentTweets");

            entity.HasKey(e => e.InstrumentTweetId);

            entity.Property(e => e.InstrumentTweetId).HasColumnName("InstrumentTweetId");
            entity.Property(e => e.InstrumentId).HasColumnName("InstrumentId");
            entity.Property(e => e.TweetType).HasColumnName("TweetType");
            entity.Property(e => e.TemplateId).HasColumnName("TemplateId");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleId");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.InstrumentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }
    }
}