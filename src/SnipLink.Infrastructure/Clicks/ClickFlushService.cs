using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnipLink.Application.Links;
using SnipLink.Domain.Entities;
using SnipLink.Infrastructure.Persistence;

namespace SnipLink.Infrastructure.Clicks;

public class ClickFlushService : BackgroundService
{
    private const int MaxBatchSize = 200;

    private readonly ChannelClickRecorder _recorder;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClickFlushService> _logger;

    public ClickFlushService(
        ChannelClickRecorder recorder,
        IServiceScopeFactory scopeFactory,
        ILogger<ClickFlushService> logger)
    {
        _recorder = recorder;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _recorder.Reader;
        var batch = new List<ClickEvent>(MaxBatchSize);

        try
        {
            while (await reader.WaitToReadAsync(stoppingToken))
            {
                while (batch.Count < MaxBatchSize && reader.TryRead(out var click))
                    batch.Add(ToEntity(click));

                if (batch.Count > 0)
                {
                    await PersistAsync(batch, stoppingToken);
                    batch.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            while (reader.TryRead(out var click))
                batch.Add(ToEntity(click));
            if (batch.Count > 0)
                await PersistAsync(batch, CancellationToken.None);
        }
    }

    private async Task PersistAsync(List<ClickEvent> batch, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ClickEvents.AddRange(batch);
            await db.SaveChangesAsync(ct);
            _logger.LogDebug("Persisted {Count} click event(s).", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist a batch of {Count} click event(s).", batch.Count);
        }
    }

    private static ClickEvent ToEntity(ClickInfo c) => new()
    {
        ShortLinkId = c.ShortLinkId,
        ClickedAt = c.ClickedAt,
        Referrer = c.Referrer,
        UserAgent = c.UserAgent,
        IpHash = c.IpHash,
        Country = c.Country
    };
}
