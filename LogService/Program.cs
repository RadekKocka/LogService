using Microsoft.EntityFrameworkCore;

namespace LogService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var cString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            builder.Services.AddDbContextFactory<SamkDBContext>(options =>
                options.UseSqlServer(cString));
            builder.Services.AddLogging();
            builder.Services.AddHostedService<Worker>();
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
