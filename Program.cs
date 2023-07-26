using MarketPulse.DbContext;
using MarketPulse.Models;
using MarketPulse.Services;
using MarketPulse.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MarketPulse
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
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
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection"));
                    }, ServiceLifetime.Singleton);
                })
                .UseWindowsService();
        }
    }
}
