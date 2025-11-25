
using Microsoft.EntityFrameworkCore;
using SamkPoolOccupancyApi.DBContext;
using SamkPoolOccupancyApi.Repository;

namespace SamkPoolOccupancyApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("AZURE_DB_CONNECTION");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    "No connection string found. Provide a connection string named 'DefaultConnection' in appsettings.json/user secrets, " +
                    "or set the 'AZURE_DB_CONNECTION' environment variable.");
                Console.ResetColor();
                return;
            }

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddDbContextFactory<SamkPoolOccupancyDBContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddTransient<IOccupancyRepository, OccupancyRepositoryImpl>();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
