using Microsoft.EntityFrameworkCore;
using SnipLink.Domain;
using SnipLink.Domain.Entities;
using SnipLink.Infrastructure.Persistence;

namespace SnipLink.Web.Infrastructure;

public static class DatabaseStartup
{
    /// <summary>Applies migrations on startup and, if enabled, seeds demo data.</summary>
    public static async Task MigrateAndSeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Relational providers get migrations applied; the in-memory test provider
        // does not support them, so just ensure the schema exists.
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        if (app.Configuration.GetValue<bool>("SnipLink:Seed"))
            await SeedAsync(db);
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        if (await db.ShortLinks.AnyAsync())
            return;

        var link = new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = Base62.Generate(),
            LongUrl = "https://learn.microsoft.com/dotnet/",
            OwnerToken = Base62.Generate(32),
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };
        db.ShortLinks.Add(link);

        // Synthetic click history so the stats page looks alive in a demo.
        var referrers = new[] { "https://news.ycombinator.com/", "https://twitter.com/", null };
        var random = new Random(42);
        for (var dayOffset = 6; dayOffset >= 0; dayOffset--)
        {
            var day = DateTime.UtcNow.Date.AddDays(-dayOffset);
            var clicksThatDay = random.Next(3, 15);
            for (var i = 0; i < clicksThatDay; i++)
            {
                db.ClickEvents.Add(new ClickEvent
                {
                    ShortLinkId = link.Id,
                    ClickedAt = day.AddMinutes(random.Next(0, 1440)),
                    Referrer = referrers[random.Next(referrers.Length)],
                    UserAgent = "Mozilla/5.0 (demo seed)",
                    IpHash = null,
                    Country = null
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
