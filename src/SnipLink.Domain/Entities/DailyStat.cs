namespace SnipLink.Domain.Entities;

/// <summary>
/// A derived daily rollup of clicks for a link, produced by the background worker.
/// Unique per (ShortLinkId, Date) so aggregation can upsert idempotently.
/// </summary>
public class DailyStat
{
    public long Id { get; set; }

    public Guid ShortLinkId { get; set; }

    /// <summary>The UTC calendar date this count covers (time component is zero).</summary>
    public DateOnly Date { get; set; }

    public int ClickCount { get; set; }

    public ShortLink? ShortLink { get; set; }
}
