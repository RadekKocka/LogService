using Microsoft.EntityFrameworkCore;
using SamkPoolOccupancyApi.DBContext;
using SamkPoolOccupancyApi.Models;

namespace SamkPoolOccupancyApi.Repository
{
    public class OccupancyRepositoryImpl : IOccupancyRepository
    {
        private readonly IDbContextFactory<SamkPoolOccupancyDBContext> _dbContextFactory;
        public OccupancyRepositoryImpl(IDbContextFactory<SamkPoolOccupancyDBContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }
        public IEnumerable<LogEntry> GetAll()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            if (dbContext == null)
                throw new Exception("Database context does not exist!");

            return [.. dbContext.LogEntries.OrderByDescending(logEntry => logEntry.Timestamp)];
        }
    }
}
