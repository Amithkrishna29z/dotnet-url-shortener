using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnipLink.Domain.Entities;

namespace SnipLink.Infrastructure.Persistence.Configurations;

public class DailyStatConfiguration : IEntityTypeConfiguration<DailyStat>
{
    public void Configure(EntityTypeBuilder<DailyStat> builder)
    {
        builder.ToTable("daily_stats");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.ClickCount).IsRequired();

        // One row per link per day; lets aggregation upsert idempotently.
        builder.HasIndex(x => new { x.ShortLinkId, x.Date }).IsUnique();
    }
}
