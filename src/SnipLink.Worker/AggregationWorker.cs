using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnipLink.Application.Abstractions;
using SnipLink.Infrastructure.Persistence;

namespace SnipLink.Worker;

/// <summary>
/// Periodically rolls raw click events up into daily stats. Runs on a configurable
/// interval and optionally prunes old raw clicks after aggregation.
/// </summary>
public class AggregationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkerOptions _options;
    private readonly ILogger<AggregationWorker> _logger;

    public AggregationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> options,
        ILogger<AggregationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Aggregation worker started; interval {Interval}.", _options.Interval);

        using var timer = new PeriodicTimer(_options.Interval);
        do
        {
            await RunCycleAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var aggregator = scope.ServiceProvider.GetRequiredService<IClickAggregator>();

            var buckets = await aggregator.AggregateAsync(ct);
            _logger.LogInformation("Aggregated {Buckets} day-bucket(s).", buckets);

            if (_options.PruneOldClicks)
                await PruneAsync(scope.ServiceProvider, ct);
        }
        catch (OperationCanceledException)
        {
            // Shutting down — nothing to do.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aggregation cycle failed.");
        }
    }

    private async Task PruneAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var cutoff = DateTime.UtcNow.AddDays(-_options.RetentionDays);
        var deleted = await db.ClickEvents
            .Where(c => c.ClickedAt < cutoff)
            .ExecuteDeleteAsync(ct);
        if (deleted > 0)
            _logger.LogInformation("Pruned {Deleted} click event(s) older than {Days} day(s).", deleted, _options.RetentionDays);
    }
}
