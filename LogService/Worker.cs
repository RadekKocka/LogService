using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace LogService;

public class Worker : BackgroundService
{
    private readonly IDbContextFactory<SamkDBContext> _dbContextFactory;
    private readonly ILogger<Worker> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    private const string SourceUrl = "https://samk.cz/aquapark-kladno";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(300);
    private static readonly TimeSpan OperatingEnd = new(20, 30, 0);


    public Worker(
        IDbContextFactory<SamkDBContext> dbContextFactory,
        ILogger<Worker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    private Task<IPlaywright> CreatePlayWright()
    {
        return Playwright.CreateAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Environment.SetEnvironmentVariable( "PLAYWRIGHT_BROWSERS_PATH",
                                            "0",
                                            EnvironmentVariableTarget.Process);

        using var timer = new PeriodicTimer(PollInterval);
        _playwright = await CreatePlayWright();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });

        _logger.LogInformation("LogService started.");

        try
        {
            do
            {
                var now = DateTime.UtcNow;

                if (!IsWithinOperatingHours(now))
                {
                    await WaitUntilOperatingStartAsync(now, stoppingToken);
                    continue;
                }

                try
                {
                    await using var context = await _browser!.NewContextAsync();
                    var page = await context.NewPageAsync();

                    await page.GotoAsync(SourceUrl, new()
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    });

                    var html = await page.ContentAsync();
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
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("LogService is stopping due to cancellation request.");
        }
        finally
        {
            timer.Dispose();
            _logger.LogInformation("LogService has stopped.");
        }
    }

    private static bool IsWithinOperatingHours(DateTime dt)
    {
        var start = GetStartTimeBasedOnDay(dt);
        var time = dt.TimeOfDay;
        return time >= start && time <= OperatingEnd;
    }

    private static TimeSpan GetStartTimeBasedOnDay(DateTime dateTime) =>
        dateTime.DayOfWeek == DayOfWeek.Monday ? new TimeSpan(11, 0, 0) : new TimeSpan(8, 30, 0);

    private static DateTime GetNextOperatingStart(DateTime now)
    {
        var todayStart = now.Date.Add(GetStartTimeBasedOnDay(now));
        if (todayStart > now)
            return todayStart;

        var tomorrow = now.Date.AddDays(1);
        return tomorrow.Add(GetStartTimeBasedOnDay(tomorrow));
    }

    private async Task WaitUntilOperatingStartAsync(DateTime now, CancellationToken ct)
    {
        var nextStart = GetNextOperatingStart(now);
        var delay = nextStart - DateTime.UtcNow;
        if (delay <= TimeSpan.Zero) return;

        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Outside operating hours. Waiting until {startTime}.", nextStart);
            await Task.Delay(delay, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {

        }
    }

    private int? ParseOccupancy(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning("HTML content is empty.");
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nameNode = doc.DocumentNode.SelectSingleNode("//div[@class='aq-chart-name']");
        if (nameNode == null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning($"Could not find 'aq-chart-name' class in HTML.");
            return null;
        }

        var name = nameNode.InnerText?.Trim();
        if (!string.Equals(name, "Bazen", StringComparison.InvariantCultureIgnoreCase) &&
            !string.Equals(name, "Bazén", StringComparison.InvariantCultureIgnoreCase))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning("Unexpected chart name '{name}'. Should be 'Bazén' or 'Bazen'.", name);
            return null;
        }

        var valueNode = nameNode.SelectSingleNode("./following-sibling::div[@class='aq-chart-value']");
        var valueText = valueNode?.InnerText?.Trim();
        if (string.IsNullOrEmpty(valueText))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning("Could not find occupancy value in HTML.");
            return null;
        }

        if (int.TryParse(valueText, out int result))
        {
            return result;
        }

        return null;
    }

    private async Task SaveLogEntryAsync(int occupancy, CancellationToken ct)
    {
        try
        {
            await using var context = _dbContextFactory.CreateDbContext();
            context.LogEntries.Add(new Classes.LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Occupancy = occupancy
            });

            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(ex, "Error saving log entry to database.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("LogService is stopping.");
        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }
        _playwright?.Dispose();
        _playwright = null;
        await base.StopAsync(cancellationToken);
    }
}
