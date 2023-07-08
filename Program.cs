using MarketPulse.DbContext;
using MarketPulse.Models;
using MarketPulse.Services;
using Microsoft.EntityFrameworkCore;

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
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<TweetService>();
                    services.AddSingleton<IRSSFeedServiceDbContextFactory, RSSFeedServiceDbContextFactory>();
                    services.AddSingleton<ITweetProperties, TweetProperties>();
                    services.AddDbContext<RSSFeedServiceDbContext>(options =>
                    {
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection"));
                    }, ServiceLifetime.Singleton);
                })
                .UseWindowsService();
        }
    }
}