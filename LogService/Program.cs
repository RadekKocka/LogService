using Microsoft.EntityFrameworkCore;

namespace LogService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContextFactory<SamkDBContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddLogging();
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddHttpClient();
            if (OperatingSystem.IsWindows())
                builder.Logging.AddEventLog();
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "Samk Pool Occupancy Log Service";
            });

            var host = builder.Build();
            host.Run();
        }
    }
}
