using SnipLink.Application.Links;

namespace SnipLink.Application.Abstractions;

public interface IAnalyticsRepository
{
    /// <summary>Total raw clicks recorded for a link (the live source of truth).</summary>
    Task<long> GetTotalClicksAsync(Guid shortLinkId, CancellationToken ct = default);

    /// <summary>Daily click series from the aggregated DailyStat rows, oldest first.</summary>
    Task<IReadOnlyList<DailyPoint>> GetDailySeriesAsync(Guid shortLinkId, CancellationToken ct = default);

    /// <summary>Top referrers by click count for a link.</summary>
    Task<IReadOnlyList<ReferrerCount>> GetTopReferrersAsync(Guid shortLinkId, int limit, CancellationToken ct = default);
}
