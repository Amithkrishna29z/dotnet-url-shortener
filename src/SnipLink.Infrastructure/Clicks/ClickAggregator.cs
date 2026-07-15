using Microsoft.EntityFrameworkCore;
using SnipLink.Application.Abstractions;
using SnipLink.Domain.Entities;
using SnipLink.Infrastructure.Persistence;

namespace SnipLink.Infrastructure.Clicks;

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
