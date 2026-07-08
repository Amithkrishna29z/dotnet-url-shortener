namespace SnipLink.Application.Abstractions;

/// <summary>
/// Rolls raw click events up into per-day counts. Implementations must be
/// idempotent: running twice over the same data yields the same DailyStat rows.
/// </summary>
public interface IClickAggregator
{
    /// <summary>Aggregate clicks into daily stats. Returns the number of day-buckets upserted.</summary>
    Task<int> AggregateAsync(CancellationToken ct = default);
}
