using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SnipLink.Domain.Entities;
using SnipLink.Infrastructure.Clicks;
using SnipLink.Infrastructure.Persistence;

namespace SnipLink.UnitTests;

public class ClickAggregatorTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static void SeedClicks(AppDbContext db, Guid linkId)
    {
        var day1 = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        var day2 = new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Utc);

        db.ClickEvents.AddRange(
            new ClickEvent { ShortLinkId = linkId, ClickedAt = day1.AddHours(1) },
            new ClickEvent { ShortLinkId = linkId, ClickedAt = day1.AddHours(5) },
            new ClickEvent { ShortLinkId = linkId, ClickedAt = day1.AddHours(9) },
            new ClickEvent { ShortLinkId = linkId, ClickedAt = day2.AddHours(2) });
        db.SaveChanges();
    }

    [Fact]
    public async Task Aggregate_ProducesPerDayCounts()
    {
        await using var db = NewContext();
        var linkId = Guid.NewGuid();
        SeedClicks(db, linkId);

        var buckets = await new ClickAggregator(db).AggregateAsync();

        buckets.Should().Be(2);
        var stats = await db.DailyStats.OrderBy(d => d.Date).ToListAsync();
        stats.Should().HaveCount(2);
        stats[0].Date.Should().Be(new DateOnly(2026, 6, 10));
        stats[0].ClickCount.Should().Be(3);
        stats[1].Date.Should().Be(new DateOnly(2026, 6, 11));
        stats[1].ClickCount.Should().Be(1);
    }

    [Fact]
    public async Task Aggregate_IsIdempotent()
    {
        await using var db = NewContext();
        var linkId = Guid.NewGuid();
        SeedClicks(db, linkId);

        await new ClickAggregator(db).AggregateAsync();
        await new ClickAggregator(db).AggregateAsync(); // run again over the same data

        var stats = await db.DailyStats.ToListAsync();
        stats.Should().HaveCount(2);
        stats.Sum(s => s.ClickCount).Should().Be(4); // not doubled
    }

    [Fact]
    public async Task Aggregate_UpdatesCountWhenNewClicksArrive()
    {
        await using var db = NewContext();
        var linkId = Guid.NewGuid();
        SeedClicks(db, linkId);
        await new ClickAggregator(db).AggregateAsync();

        db.ClickEvents.Add(new ClickEvent
        {
            ShortLinkId = linkId,
            ClickedAt = new DateTime(2026, 6, 10, 20, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        await new ClickAggregator(db).AggregateAsync();

        var day1 = await db.DailyStats.SingleAsync(d => d.Date == new DateOnly(2026, 6, 10));
        day1.ClickCount.Should().Be(4);
    }

    [Fact]
    public async Task Aggregate_WithNoClicks_ReturnsZero()
    {
        await using var db = NewContext();

        var buckets = await new ClickAggregator(db).AggregateAsync();

        buckets.Should().Be(0);
    }
}
