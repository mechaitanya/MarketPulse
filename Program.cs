using MarketPulse.DbContext;
using MarketPulse.Models;
using MarketPulse.Services;
using MarketPulse.Utility;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

namespace MarketPulse
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();

            try
            {
                Log.Information("Starting Marketpulse...");

                CreateHostBuilder(args).Build().Run();

                Log.Information("Marketpulse Stopped...");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.SetBasePath(hostContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();
                })
                 .ConfigureLogging((hostContext, logging) =>
                 {
                     logging.ClearProviders();
                     logging.AddSerilog();
                 })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMemoryCache();
                    services.AddHostedService<TweetService>();
                    services.AddScoped<CacheUpdatingService>();
                    services.AddSingleton<IRSSFeedServiceDbContextFactory, RSSFeedServiceDbContextFactory>();
                    services.AddSingleton<IMyLogger, MyLogger>();
                    services.AddSingleton<IEmailSender, EmailSender>();
                    services.AddSingleton<ITweetProperties, TweetProperties>();
                    services.AddScoped<CreateImage>();
                    services.Configure<AppSettings>(hostContext.Configuration.GetSection("AppSettings"));
                    services.AddScoped<AppConfig>();
                    services.AddDbContext<RSSFeedServiceDbContext>(options =>
                    {
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("FeedServiceConnectionString"));
                    }, ServiceLifetime.Singleton);
                })
                .UseWindowsService();
        }
    }
}