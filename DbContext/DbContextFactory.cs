using Microsoft.EntityFrameworkCore;

namespace MarketPulse.DbContext
{
    public interface IRSSFeedServiceDbContextFactory
    {
        RSSFeedServiceDbContext CreateDbContext();
    }

    public class RSSFeedServiceDbContextFactory : IRSSFeedServiceDbContextFactory
    {
        private readonly DbContextOptions<RSSFeedServiceDbContext> _dbContextOptions;

        public RSSFeedServiceDbContextFactory(DbContextOptions<RSSFeedServiceDbContext> dbContextOptions)
        {
            _dbContextOptions = dbContextOptions;
        }

        public RSSFeedServiceDbContext CreateDbContext()
        {
            return new RSSFeedServiceDbContext(_dbContextOptions);
        }
    }
}