using LogService.Classes;
using Microsoft.EntityFrameworkCore;

namespace LogService;

public class SamkDBContext : DbContext
{
    public SamkDBContext(DbContextOptions<SamkDBContext> options) : base(options)
    {
    }

    public DbSet<LogEntry> LogEntries { get; set; }
}
