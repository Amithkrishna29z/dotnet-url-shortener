using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnipLink.Domain.Entities;

namespace SnipLink.Infrastructure.Persistence.Configurations;

public class ClickEventConfiguration : IEntityTypeConfiguration<ClickEvent>
{
    public void Configure(EntityTypeBuilder<ClickEvent> builder)
    {
        builder.ToTable("click_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClickedAt).IsRequired();
        builder.Property(x => x.Referrer).HasMaxLength(2048);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.IpHash).HasMaxLength(64);
        builder.Property(x => x.Country).HasMaxLength(2);

        // Supports the worker's "new clicks for a link in a date range" scans.
        builder.HasIndex(x => new { x.ShortLinkId, x.ClickedAt });
    }
}
