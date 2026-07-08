using Microsoft.EntityFrameworkCore;
using SnipLink.Application.Abstractions;
using SnipLink.Domain.Entities;
using SnipLink.Infrastructure.Persistence;

namespace SnipLink.Infrastructure.Clicks;

/// <summary>
/// Recomputes per-(link, day) click counts from the raw ClickEvent rows and upserts
/// them into DailyStat. Idempotent by design: it sets each day-bucket's count to the
/// recomputed total, so re-running produces identical results.
///
/// Tradeoff: this recomputes counts over all click history each cycle, which is fine
/// for a demo-scale dataset. A production version would track a watermark and only
/// process new rows — noted as future work in the README.
/// </summary>
public class ClickAggregator : IClickAggregator
{
    private readonly AppDbContext _db;

    public ClickAggregator(AppDbContext db) => _db = db;

    public async Task<int> AggregateAsync(CancellationToken ct = default)
    {
        var counts = await _db.ClickEvents
            .GroupBy(c => new { c.ShortLinkId, Day = c.ClickedAt.Date })
            .Select(g => new { g.Key.ShortLinkId, g.Key.Day, Count = g.Count() })
            .ToListAsync(ct);

        if (counts.Count == 0)
            return 0;

        var existing = await _db.DailyStats.ToListAsync(ct);
        var byKey = existing.ToDictionary(d => (d.ShortLinkId, d.Date));

        foreach (var c in counts)
        {
            var date = DateOnly.FromDateTime(c.Day);
            if (byKey.TryGetValue((c.ShortLinkId, date), out var stat))
            {
                stat.ClickCount = c.Count;
            }
            else
            {
                _db.DailyStats.Add(new DailyStat
                {
                    ShortLinkId = c.ShortLinkId,
                    Date = date,
                    ClickCount = c.Count
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return counts.Count;
    }
}
