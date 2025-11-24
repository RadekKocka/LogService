namespace SamkPoolOccupancyApi.DBContext;
using Microsoft.EntityFrameworkCore;
using SamkPoolOccupancyApi.Models;
using Microsoft.EntityFrameworkCore.SqlServer;

public class SamkPoolOccupancyDBContext : DbContext
{
    public SamkPoolOccupancyDBContext(DbContextOptions<SamkPoolOccupancyDBContext> options) : base(options)
    {
    }
    public DbSet<LogEntry> LogEntries { get; set; }
}
