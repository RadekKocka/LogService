using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace LogService;

public class Worker : BackgroundService
{
    private readonly HttpClient _httpClient;
    private readonly IDbContextFactory<SamkDBContext> _dbContextFactory;

    private const string SourceUrl = "https://samk.cz/aquapark-kladno";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan OperatingEnd = new(21, 30, 0);

    public Worker(
        IHttpClientFactory httpClientFactory,
        IDbContextFactory<SamkDBContext> dbContextFactory,
        ILogger<Worker> logger)
    {
        _httpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        try
        {
            do
            {
                var now = DateTime.Now;

                if (!IsWithinOperatingHours(now))
                {
                    await WaitUntilOperatingStartAsync(now, stoppingToken);
                    continue;
                }

                try
                {
                    var html = await _httpClient.GetStringAsync(SourceUrl, stoppingToken);
                    var occupancy = ParseOccupancy(html);

                    if (occupancy.HasValue)
                    {
                        await SaveLogEntryAsync(occupancy.Value, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {

        }
    }

    private static bool IsWithinOperatingHours(DateTime dt)
    {
        var start = GetStartTimeBasedOnDay(dt);
        var time = dt.TimeOfDay;
        return time >= start && time <= OperatingEnd;
    }

    private static TimeSpan GetStartTimeBasedOnDay(DateTime dateTime) =>
        dateTime.DayOfWeek == DayOfWeek.Monday ? new TimeSpan(12, 0, 0) : new TimeSpan(9, 30, 0);

    private static DateTime GetNextOperatingStart(DateTime now)
    {
        var todayStart = now.Date.Add(GetStartTimeBasedOnDay(now));
        if (todayStart > now) return todayStart;

        var tomorrow = now.Date.AddDays(1);
        return tomorrow.Add(GetStartTimeBasedOnDay(tomorrow));
    }

    private static async Task WaitUntilOperatingStartAsync(DateTime now, CancellationToken ct)
    {
        var nextStart = GetNextOperatingStart(now);
        var delay = nextStart - DateTime.Now;
        if (delay <= TimeSpan.Zero) return;

        try
        {
            await Task.Delay(delay, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private static int? ParseOccupancy(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nameNode = doc.DocumentNode.SelectSingleNode("//div[@class='aq-chart-name']");
        if (nameNode == null) return null;

        var name = nameNode.InnerText?.Trim();
        if (!string.Equals(name, "Bazen", StringComparison.InvariantCultureIgnoreCase) &&
            !string.Equals(name, "Bazén", StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        var valueNode = nameNode.SelectSingleNode("./following-sibling::div[@class='aq-chart-value']");
        var valueText = valueNode?.InnerText?.Trim();
        if (string.IsNullOrEmpty(valueText)) return null;

        if (int.TryParse(valueText, out int result))
        {
            return result;
        }

        return null;
    }

    private async Task SaveLogEntryAsync(int occupancy, CancellationToken ct)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        context.LogEntries.Add(new Classes.LogEntry
        {
            Timestamp = DateTime.Now,
            Occupancy = occupancy
        });

        await context.SaveChangesAsync(ct);
    }
}
