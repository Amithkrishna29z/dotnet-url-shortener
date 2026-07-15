using Microsoft.EntityFrameworkCore;
using SnipLink.Application.Abstractions;
using SnipLink.Application.Links;

namespace SnipLink.Infrastructure.Persistence;

public class AnalyticsRepository : IAnalyticsRepository
{
    private const string UnknownReferrer = "(direct)";

    private readonly AppDbContext _db;

    public AnalyticsRepository(AppDbContext db) => _db = db;

    public async Task<long> GetTotalClicksAsync(Guid shortLinkId, CancellationToken ct = default) =>
        await _db.ClickEvents.LongCountAsync(c => c.ShortLinkId == shortLinkId, ct);

    public async Task<IReadOnlyList<DailyPoint>> GetDailySeriesAsync(Guid shortLinkId, CancellationToken ct = default) =>
        await _db.DailyStats
            .Where(d => d.ShortLinkId == shortLinkId)
            .OrderBy(d => d.Date)
            .Select(d => new DailyPoint(d.Date, d.ClickCount))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ReferrerCount>> GetTopReferrersAsync(Guid shortLinkId, int limit, CancellationToken ct = default)
    {
        var grouped = await _db.ClickEvents
            .Where(c => c.ShortLinkId == shortLinkId)
            .GroupBy(c => c.Referrer)
            .Select(g => new { Referrer = g.Key, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync(ct);

        return grouped
            .Select(x => new ReferrerCount(x.Referrer ?? UnknownReferrer, x.Count))
            .ToList();
    }
}
