using SnipLink.Application.Links;

namespace SnipLink.Application.Abstractions;

public interface IAnalyticsRepository
{
    Task<long> GetTotalClicksAsync(Guid shortLinkId, CancellationToken ct = default);

    Task<IReadOnlyList<DailyPoint>> GetDailySeriesAsync(Guid shortLinkId, CancellationToken ct = default);

    Task<IReadOnlyList<ReferrerCount>> GetTopReferrersAsync(Guid shortLinkId, int limit, CancellationToken ct = default);
}
