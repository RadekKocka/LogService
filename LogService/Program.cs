using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace LogService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    "No connection string found. Provide a connection string named 'DefaultConnection' in appsettings.json/user secrets, " +
                    "or set the 'DB_CONNECTION_STRING' environment variable.");
                Console.ResetColor();
                return;
            }

            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddDbContextFactory<SamkDBContext>(options =>
                options.UseSqlServer(connectionString));
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
